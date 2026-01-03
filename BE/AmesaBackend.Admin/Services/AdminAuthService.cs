using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Models;
using BCrypt.Net;
using AmesaBackend.Auth.Services;
using System.Collections.Concurrent;
using System.Linq;
using System;

namespace AmesaBackend.Admin.Services
{
    /// <summary>
    /// Service for managing admin authentication and authorization.
    /// Handles admin user authentication, session management, rate limiting, and token-based security.
    /// Supports both database-backed authentication and legacy configuration-based authentication.
    /// </summary>
    public class AdminAuthService : IAdminAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminAuthService> _logger;
        private readonly AdminDbContext? _adminDbContext;
        
        // Rate limiting: track failed login attempts
        private static readonly ConcurrentDictionary<string, FailedLoginAttempt> _failedAttempts = new();
        private const int MaxFailedAttempts = 5;
        private const int LockoutDurationMinutes = 30;
        
        // In-memory authentication token cache (works even when response has started)
        // Token -> (Email, Expiry) mapping
        private static readonly ConcurrentDictionary<string, AuthTokenInfo> _authTokens = new();
        // Session ID -> Token mapping (fallback when cookies can't be set)
        private static readonly ConcurrentDictionary<string, string> _sessionIdToToken = new();
        // Email -> Token mapping (additional fallback - allows lookup by email from session/cookie)
        private static readonly ConcurrentDictionary<string, string> _emailToToken = new();
        private const int AuthTokenExpiryHours = 2; // Match session timeout
        private const string AuthTokenCookieName = "AdminAuthToken";
        private const string AuthTokenHeaderName = "X-Admin-Auth-Token";
        
        /// <summary>
        /// Represents a failed login attempt record for rate limiting.
        /// </summary>
        private class FailedLoginAttempt
        {
            /// <summary>
            /// Gets or sets the count of failed attempts.
            /// </summary>
            public int Count { get; set; }
            
            /// <summary>
            /// Gets or sets the date and time when the account lockout expires.
            /// </summary>
            public DateTime? LockedUntil { get; set; }
        }
        
        /// <summary>
        /// Represents authentication token information stored in memory cache.
        /// </summary>
        private class AuthTokenInfo
        {
            /// <summary>
            /// Gets or sets the email address of the authenticated admin user.
            /// </summary>
            public string Email { get; set; } = string.Empty;
            
            /// <summary>
            /// Gets or sets the date and time when the token expires.
            /// </summary>
            public DateTime ExpiresAt { get; set; }
            
            /// <summary>
            /// Gets or sets the date and time when the token was created.
            /// </summary>
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminAuthService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor for accessing request/response and session.</param>
        /// <param name="configuration">Configuration for reading admin settings and connection strings.</param>
        /// <param name="logger">Logger instance for logging operations.</param>
        /// <param name="serviceProvider">Service provider for resolving AdminDbContext (may be null if not configured).</param>
        public AdminAuthService(
            IHttpContextAccessor httpContextAccessor, 
            IConfiguration configuration, 
            ILogger<AdminAuthService> logger,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
            
            // Try to get AdminDbContext from service provider (may be null if connection string not configured)
            try
            {
                _adminDbContext = serviceProvider.GetService<AdminDbContext>();
                if (_adminDbContext == null)
                {
                    _logger.LogWarning("AdminDbContext is not registered - database authentication will be unavailable. Check DB_CONNECTION_STRING environment variable.");
                }
                else
                {
                    _logger.LogDebug("AdminDbContext successfully resolved from service provider");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve AdminDbContext from service provider");
                _adminDbContext = null;
            }
        }

        /// <summary>
        /// Authenticates an admin user with email and password credentials.
        /// Implements rate limiting, account lockout, and supports both database and legacy configuration-based authentication.
        /// </summary>
        /// <param name="email">The email address of the admin user.</param>
        /// <param name="password">The password of the admin user.</param>
        /// <returns>True if authentication is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method:
        /// - Checks for account lockout due to failed attempts
        /// - Validates credentials against the admin_users table in the amesa_admin schema
        /// - Falls back to legacy configuration-based auth if database user not found
        /// - Uses BCrypt for password verification
        /// - Implements rate limiting (5 attempts, 30-minute lockout)
        /// - Creates authentication tokens and stores them in memory cache, session, and cookies
        /// </remarks>
        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                _logger.LogDebug("Login attempt for email: {Email}", email);

                // Check for account lockout
                if (IsAccountLocked(normalizedEmail))
                {
                    _logger.LogWarning("Login attempt for locked account: {Email}", email);
                    return false;
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Login attempt with empty email or password");
                    RecordFailedAttempt(normalizedEmail);
                    return false;
                }

                // Try to find admin user in database (if DbContext is available)
                AdminUser? adminUser = null;
                
                // CRITICAL: Explicitly check DbContext availability before attempting authentication
                if (_adminDbContext == null)
                {
                    _logger.LogError("Authentication failed: AdminDbContext is not available for email: {Email}. Check DB_CONNECTION_STRING environment variable. Database authentication cannot proceed.", email);
                    RecordFailedAttempt(normalizedEmail);
                    return false; // Don't fall back to legacy auth - make database availability explicit
                }
                
                    try
                    {
                        _logger.LogDebug("Querying database for admin user: {Email} (normalized: {NormalizedEmail})", email, normalizedEmail);
                        
                    // Use EF Core LINQ query with proper column mappings
                    // Column mappings are configured in AdminDbContext.OnModelCreating
                    // Use EF.Functions.ILike for case-insensitive exact match in PostgreSQL
                    // ILike performs case-insensitive comparison (equivalent to SQL LOWER() comparison)
                    // normalizedEmail is already lowercase and trimmed in C#
                    // Note: If database emails have whitespace, they should be trimmed at insert time
                        adminUser = await _adminDbContext.AdminUsers
                        .Where(u => EF.Functions.ILike(u.Email, normalizedEmail) && u.IsActive)
                            .AsNoTracking()
                            .FirstOrDefaultAsync();
                        
                        if (adminUser == null)
                        {
                        _logger.LogWarning("Admin user not found in database for email: {Email} (normalized: {NormalizedEmail}). Checking if any admin users exist...", email, normalizedEmail);
                        
                        // Debug: Check if any admin users exist at all
                        var totalUsers = await _adminDbContext.AdminUsers.CountAsync();
                        _logger.LogDebug("Total admin users in database: {TotalUsers}", totalUsers);
                        
                        if (totalUsers > 0)
                        {
                            // List first few emails for debugging
                            var sampleEmails = await _adminDbContext.AdminUsers
                                .Select(u => u.Email)
                                .Take(5)
                                .ToListAsync();
                            _logger.LogDebug("Sample admin user emails in database: {Emails}", string.Join(", ", sampleEmails));
                        }
                        else
                        {
                            _logger.LogWarning("No admin users found in database. Admin user seeding may be required.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Admin user found in database for email: {Email}, is_active: {IsActive}, has_password_hash: {HasHash}, hash_length: {HashLength}", 
                            email, adminUser.IsActive, !string.IsNullOrEmpty(adminUser.PasswordHash), adminUser.PasswordHash?.Length ?? 0);
                        }
                    }
                    catch (Exception dbEx)
                    {
                    // CRITICAL: Database errors are system errors - don't silently fall back to legacy auth
                    _logger.LogError(dbEx, "Database error during authentication for email: {Email}. Exception type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}. User authentication cannot proceed due to database error.", 
                        email, dbEx.GetType().Name, dbEx.Message, dbEx.InnerException?.Message ?? "None");
                    RecordFailedAttempt(normalizedEmail);
                    return false; // Don't fall back - database error is a system error, not a user error
                }

                bool authenticated = false;

                // Check if user was found in database
                if (adminUser != null)
                {
                    // Verify password using BCrypt for database user
                    if (string.IsNullOrEmpty(adminUser.PasswordHash))
                    {
                        _logger.LogWarning("Admin user {Email} has no password hash", email);
                        RecordFailedAttempt(normalizedEmail);
                        return false;
                    }

                    _logger.LogDebug("Verifying password for admin user {Email}, hash length: {HashLength}, hash prefix: {HashPrefix}", 
                        email, adminUser.PasswordHash?.Length ?? 0, adminUser.PasswordHash?.Substring(0, Math.Min(10, adminUser.PasswordHash?.Length ?? 0)) ?? "null");
                    
                    try
                    {
                        var passwordToVerify = password.Trim();
                        
                        // Validate password hash format before attempting verification
                        if (string.IsNullOrEmpty(adminUser.PasswordHash) || !adminUser.PasswordHash.StartsWith("$2"))
                        {
                            _logger.LogError("Invalid password hash format for user {Email}. Hash does not start with BCrypt prefix ($2a$, $2b$, $2y$). Hash prefix: {HashPrefix}", 
                                email, adminUser.PasswordHash?.Substring(0, Math.Min(10, adminUser.PasswordHash?.Length ?? 0)) ?? "null");
                            RecordFailedAttempt(normalizedEmail);
                            return false;
                        }
                        
                        var passwordValid = BCrypt.Net.BCrypt.Verify(passwordToVerify, adminUser.PasswordHash);
                        
                        _logger.LogDebug("BCrypt verification result for {Email}: {IsValid} (password length: {PasswordLength})", 
                            email, passwordValid, passwordToVerify.Length);
                        
                        if (passwordValid)
                        {
                            authenticated = true;
                            
                            // Update last login time
                            if (_adminDbContext != null)
                            {
                                try
                                {
                                                  // Update last_login_at using ExecuteSqlInterpolated for proper parameterization
                                                  // This avoids tracking issues and uses SQL parameters (prevents SQL injection)
                                                  // Must pass FormattableString directly (not assign to var first)
                                                  var updateResult = await _adminDbContext.Database.ExecuteSqlInterpolatedAsync($@"
                                                      UPDATE amesa_admin.admin_users 
                                                      SET last_login_at = {DateTime.UtcNow} 
                                                      WHERE id = {adminUser.Id}");
                                    _logger.LogDebug("Updated last login time for user {Email} (rows affected: {RowsAffected})", email, updateResult);
                                }
                                catch (Exception saveEx)
                                {
                                    _logger.LogWarning(saveEx, "Failed to update last login time for user {Email}, but authentication succeeded", email);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid password for admin user: {Email} (password hash verification failed). Password provided length: {PasswordLength}", 
                                email, passwordToVerify.Length);
                            RecordFailedAttempt(normalizedEmail);
                            return false;
                        }
                    }
                    catch (Exception bcryptEx)
                    {
                        _logger.LogError(bcryptEx, "BCrypt verification exception for user {Email}: {Message}, StackTrace: {StackTrace}", 
                            email, bcryptEx.Message, bcryptEx.StackTrace);
                        RecordFailedAttempt(normalizedEmail);
                        return false;
                    }
                }
                else
                {
                    // User not found in database - fall back to legacy config-based auth ONLY if explicitly configured
                    // SECURITY: Do not use hardcoded defaults
                    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") 
                        ?? _configuration["AdminSettings:Email"];
                    
                    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") 
                        ?? _configuration["AdminSettings:Password"];

                    // Only allow legacy auth if both email and password are explicitly configured
                    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                    {
                        var emailMatch = normalizedEmail == adminEmail.ToLower().Trim();
                        var passwordMatch = password.Trim() == adminPassword.Trim();

                        if (emailMatch && passwordMatch)
                        {
                            _logger.LogInformation("Authenticated using legacy config-based credentials for email: {Email}", email);
                            authenticated = true;
                        }
                        else
                        {
                            _logger.LogWarning("Authentication failed for email: {Email} - Legacy credentials don't match", email);
                            RecordFailedAttempt(normalizedEmail);
                            return false;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Authentication failed for email: {Email} - User not found in database and legacy auth not configured", email);
                        RecordFailedAttempt(normalizedEmail);
                        return false;
                    }
                }

                if (authenticated)
                {
                    // Clear failed attempts on successful login
                    ClearFailedAttempts(normalizedEmail);
                    _logger.LogInformation("Admin user {Email} authenticated successfully", email);
                    return await SetAuthenticationSuccess(email);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during authentication for email: {Email}", email);
                return false;
            }
        }

        private async Task<bool> SetAuthenticationSuccess(string email)
        {
            // CRITICAL FIX: Generate and store token in memory FIRST, before any HttpContext checks
            // This ensures authentication succeeds even if HttpContext is null (can happen in Blazor Server)
            // In-memory token cache works independently of HttpContext
            
            // STEP 1: ALWAYS generate and store token in memory (works without HttpContext)
            var token = GenerateSecureToken();
            var tokenInfo = new AuthTokenInfo
            {
                Email = email,
                ExpiresAt = DateTime.UtcNow.AddHours(AuthTokenExpiryHours),
                CreatedAt = DateTime.UtcNow
            };
            _authTokens[token] = tokenInfo;
            
            // STEP 2: ALWAYS map email to token (works regardless of HttpContext)
            // This allows token retrieval via email lookup even when cookies/session aren't available
            // NOTE: This overwrites previous token mapping for the same email, effectively invalidating old sessions
            // which is a security feature (single active session per email, though old tokens remain valid until expiry)
            var normalizedEmail = email.ToLower().Trim();
            
            // If email already has a token mapped, log it (old token still valid until expiry, but email lookup will use new token)
            if (_emailToToken.TryGetValue(normalizedEmail, out var oldToken))
            {
                _logger.LogWarning("User {Email} logged in again. Previous token mapping overwritten (old token still valid until expiry). New token: {NewTokenPrefix}...", 
                    email, token.Substring(0, Math.Min(8, token.Length)));
            }
            
            _emailToToken[normalizedEmail] = token;
            _logger.LogWarning("CRITICAL: Token generated and stored in memory for user {Email}. Email mapped to token. Token: {TokenPrefix}...", email, token.Substring(0, Math.Min(8, token.Length)));
            
            // STEP 3: Clean up expired tokens
            CleanupExpiredTokens();
            
            // STEP 4: Try to use HttpContext for additional storage (session/cookies) if available
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                // HttpContext is null - this can happen in Blazor Server component handlers
                // But we've stored the token in memory, so authentication should succeed
                _logger.LogWarning("HttpContext is null for user {Email}. Token stored in memory cache with email mapping. Authentication will work via email lookup on subsequent requests.", email);
                return true; // Return true because token is stored in memory
            }
            
            // CRITICAL: Access session EARLY to ensure session cookie is set before response starts
            // This must happen BEFORE we try to write to session, so the cookie is available for subsequent requests
            string? sessionId = null;
            if (httpContext.Session != null)
            {
                try
                {
                    // Force session load early to set session cookie
                    if (!httpContext.Session.IsAvailable)
                    {
                        await httpContext.Session.LoadAsync();
                    }
                    // Access Session.Id to ensure session cookie is set
                    sessionId = httpContext.Session.Id;
                    _logger.LogWarning("Early session access: Session ID {SessionId} retrieved for user {Email}", sessionId, email);
                }
                catch (Exception earlySessionEx)
                {
                    _logger.LogWarning(earlySessionEx, "Failed to access session early for user {Email}, but continuing", email);
                }
            }
            
            // STEP 5: Map session ID to token (additional fallback mechanism)
            // Session ID persists across requests via ASP.NET Core session cookie
            try
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _sessionIdToToken[sessionId] = token;
                    _logger.LogWarning("Mapped session ID {SessionId} to token for user {Email}", sessionId, email);
                }
                else if (httpContext.Session != null)
                {
                    // Fallback: Try to get session ID again if we didn't get it earlier
                    try
                    {
                        if (!httpContext.Session.IsAvailable)
                        {
                            await httpContext.Session.LoadAsync();
                        }
                        sessionId = httpContext.Session.Id;
                    }
                    catch
                    {
                        // Session ID unavailable - that's OK, email mapping will handle it
                    }
                    
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _sessionIdToToken[sessionId] = token;
                        _logger.LogWarning("Mapped session ID {SessionId} to token for user {Email} (fallback)", sessionId, email);
                    }
                    else
                    {
                        _logger.LogWarning("Session ID was empty when trying to map token for user {Email}. Token stored in cache with email mapping, session ID mapping failed.", email);
                    }
                }
            }
            catch (Exception sessionIdEx)
            {
                // Session ID mapping failed - log but don't fail authentication
                // Email mapping is already done, so authentication can still work
                _logger.LogDebug(sessionIdEx, "Failed to map session ID to token for user {Email}. Token is still stored in cache with email mapping.", email);
            }
            
            _logger.LogDebug("Generated authentication token for user {Email}. Token stored in memory cache with email and session ID mappings.", email);
            
            try
            {
                // Try to store in session if response hasn't started
                if (!httpContext.Response.HasStarted && httpContext.Session != null)
                {
                    try
                    {
                        if (!httpContext.Session.IsAvailable)
                        {
                            await httpContext.Session.LoadAsync();
                        }
                        
                        if (httpContext.Session.IsAvailable)
                        {
                            // Double-check response hasn't started during async LoadAsync
                            if (!httpContext.Response.HasStarted)
                            {
                                httpContext.Session.SetString("AdminEmail", email);
                                httpContext.Session.SetString("AdminLoginTime", DateTime.UtcNow.ToString("O"));
                                httpContext.Session.SetString("AdminAuthToken", token); // Store token in session too
                                httpContext.Session.CommitAsync().GetAwaiter().GetResult();
                                
                                _logger.LogDebug("Authentication state stored in session for user {Email}", email);
                            }
                        }
                    }
                    catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("response has started"))
                    {
                        // Response started during session write - token cache will handle it
                        _logger.LogDebug("Response started during session write for user {Email}. Using token cache only.", email);
                    }
                    catch (Exception sessionEx)
                    {
                        _logger.LogWarning(sessionEx, "Failed to store session for user {Email}. Using token cache only.", email);
                    }
                }
                
                // Try to store token in cookie if response hasn't started
                if (!httpContext.Response.HasStarted)
                {
                    try
                    {
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = !httpContext.Request.IsHttps ? false : true,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddHours(AuthTokenExpiryHours)
                        };
                        
                        httpContext.Response.Cookies.Append(AuthTokenCookieName, token, cookieOptions);
                        _logger.LogDebug("Authentication token stored in cookie for user {Email}", email);
                    }
                    catch (Exception cookieEx)
                    {
                        // Cookie setting failed, but token is in memory cache - that's OK
                        _logger.LogDebug(cookieEx, "Failed to store token in cookie for user {Email}. Using memory cache only.", email);
                    }
                }
                else
                {
                    _logger.LogDebug("Response has already started for user {Email}. Token stored in memory cache only. Client should include token in subsequent requests.", email);
                }
            }
            catch (Exception ex)
            {
                // Even if session/cookie storage fails, token is in memory - authentication can still work
                _logger.LogWarning(ex, "Failed to store authentication state in session/cookies for user {Email}. Token is stored in memory cache.", email);
            }
            
            
            // STEP 7: Store token in response headers so JavaScript can read it (if response hasn't started)
            // Headers can sometimes be modified after response starts, but we check for safety and consistency
            try
            {
                if (!httpContext.Response.HasStarted)
                {
                    httpContext.Response.Headers.Append("X-Admin-Auth-Token", token);
                    _logger.LogInformation("Token stored in response header X-Admin-Auth-Token for client-side retrieval");
                }
                else
                {
                    _logger.LogDebug("Response already started, cannot store token in header for user {Email}. Email mapping will handle token retrieval.", email);
                }
            }
            catch (Exception headerEx)
            {
                // Header storage failed - not critical, email mapping will handle token retrieval
                _logger.LogWarning(headerEx, "Failed to store token in response header for user {Email}. Email mapping will handle token retrieval.", email);
            }
            
            return true;
        }
        
        private string GenerateSecureToken()
        {
            // Generate a secure random token
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }
        
        private void CleanupExpiredTokens()
        {
            // Simple cleanup - remove expired tokens (runs on every token creation)
            // More sophisticated cleanup could use a background task
            var now = DateTime.UtcNow;
            
            // Get expired tokens WITH their info BEFORE removing them (critical fix)
            var expiredTokens = _authTokens
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => (token: kvp.Key, info: kvp.Value))
                .ToList();
            
            foreach (var (token, tokenInfo) in expiredTokens)
            {
                // Remove from main token cache
                _authTokens.TryRemove(token, out _);
                
                // Remove from session ID mapping
                var sessionIdMappings = _sessionIdToToken
                    .Where(kvp => kvp.Value == token)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var sessionId in sessionIdMappings)
                {
                    _sessionIdToToken.TryRemove(sessionId, out _);
                }
                
                // Remove from email mapping using the tokenInfo we got before removal
                _emailToToken.TryRemove(tokenInfo.Email.ToLower().Trim(), out _);
            }
            
            if (expiredTokens.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired authentication tokens and their mappings", expiredTokens.Count);
            }
        }
        
        private string? GetAuthTokenFromRequest()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;
            
            // Try to get token from cookie first
            var token = httpContext.Request.Cookies[AuthTokenCookieName];
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
            
            // Try to get token from header (for API calls or client-side storage)
            if (httpContext.Request.Headers.TryGetValue(AuthTokenHeaderName, out var headerToken))
            {
                var tokenValue = headerToken.ToString();
                if (!string.IsNullOrEmpty(tokenValue))
                {
                    _logger.LogInformation("GetAuthTokenFromRequest: Found token in header X-Admin-Auth-Token");
                    return tokenValue;
                }
            }
            
            // Also check X-Admin-Auth-Token header (alternative name for client-side)
            if (httpContext.Request.Headers.TryGetValue("X-Admin-Auth-Token", out var altHeaderToken))
            {
                var tokenValue = altHeaderToken.ToString();
                if (!string.IsNullOrEmpty(tokenValue))
                {
                    _logger.LogInformation("GetAuthTokenFromRequest: Found token in header X-Admin-Auth-Token (alt)");
                    return tokenValue;
                }
            }
            
            // Try to get token from session storage (if available)
            if (httpContext.Session != null && httpContext.Session.IsAvailable)
            {
                try
                {
                    token = httpContext.Session.GetString("AdminAuthToken");
                    if (!string.IsNullOrEmpty(token) && _authTokens.ContainsKey(token))
                    {
                        return token;
                    }
                }
                catch
                {
                    // Session not available or error reading - continue to session ID lookup
                }
            }
            
            // FALLBACK 1: Try to get token using session ID mapping (works even when cookies aren't set)
            // This is critical for Blazor Server when response has started and cookies can't be set
            // Session ID persists across requests via ASP.NET Core session cookie
            if (httpContext.Session != null)
            {
                try
                {
                    // Ensure session is loaded to get Session.Id
                    // This is critical - Session.Id may not be available until session is loaded
                    if (!httpContext.Session.IsAvailable)
                    {
                        try
                        {
                            // Try to load session synchronously
                            httpContext.Session.LoadAsync().GetAwaiter().GetResult();
                        }
                        catch
                        {
                            // Session load failed - Session.Id might still work
                        }
                    }
                    
                    var sessionId = httpContext.Session.Id;
                    _logger.LogInformation("GetAuthTokenFromRequest: Checking session ID {SessionId} for token mapping", sessionId);
                    if (!string.IsNullOrEmpty(sessionId) && _sessionIdToToken.TryGetValue(sessionId, out token))
                    {
                        // Verify token still exists and is valid
                        if (!string.IsNullOrEmpty(token) && _authTokens.TryGetValue(token, out var tokenInfo))
                        {
                        if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                        {
                            _logger.LogInformation("Retrieved authentication token using session ID fallback for session {SessionId}, user {Email}", sessionId, tokenInfo.Email);
                            return token;
                        }
                            else
                            {
                                // Token expired - remove mapping
                                _sessionIdToToken.TryRemove(sessionId, out _);
                                _logger.LogDebug("Token expired for session ID {SessionId}, removed mapping", sessionId);
                            }
                        }
                        else
                        {
                            // Token not found in cache - remove stale mapping
                            _sessionIdToToken.TryRemove(sessionId, out _);
                        }
                    }
                }
                catch (Exception sessionIdEx)
                {
                    // Session ID retrieval failed - log debug but continue to email lookup
                    _logger.LogDebug(sessionIdEx, "Failed to retrieve token using session ID fallback");
                }
            }
            
            // FALLBACK 2: Try to get token using email mapping (if we can get email from session/cookie)
            // This works when session ID changed but we still have email in session/cookie
            try
            {
                string? email = null;
                
                // Try to get email from session first
                if (httpContext.Session != null && httpContext.Session.IsAvailable)
                {
                    try
                    {
                        email = httpContext.Session.GetString("AdminEmail");
                    }
                    catch
                    {
                        // Session read failed - try cookie
                    }
                }
                
                // Try to get email from legacy cookie if session didn't work
                if (string.IsNullOrEmpty(email))
                {
                    email = httpContext.Request.Cookies["AdminEmail"];
                }
                
                // If we have email, try to find token
                if (!string.IsNullOrEmpty(email))
                {
                    var normalizedEmail = email.ToLower().Trim();
                    if (_emailToToken.TryGetValue(normalizedEmail, out token))
                    {
                        // Verify token still exists and is valid
                        if (!string.IsNullOrEmpty(token) && _authTokens.TryGetValue(token, out var tokenInfo))
                        {
                            if (tokenInfo.ExpiresAt > DateTime.UtcNow && tokenInfo.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Retrieved authentication token using email fallback for {Email}", email);
                                return token;
                            }
                            else
                            {
                                // Token expired or email mismatch - remove mapping
                                _emailToToken.TryRemove(normalizedEmail, out _);
                            }
                        }
                        else
                        {
                            // Token not found in cache - remove stale mapping
                            _emailToToken.TryRemove(normalizedEmail, out _);
                        }
                    }
                }
            }
            catch (Exception emailLookupEx)
            {
                // Email lookup failed - log debug but continue
                _logger.LogDebug(emailLookupEx, "Failed to retrieve token using email fallback");
            }
            
            return null;
        }

        private bool IsAccountLocked(string normalizedEmail)
        {
            if (_failedAttempts.TryGetValue(normalizedEmail, out var attempt))
            {
                if (attempt.LockedUntil.HasValue && attempt.LockedUntil.Value > DateTime.UtcNow)
                {
                    return true;
                }
                // Lockout expired, clear it
                if (attempt.LockedUntil.HasValue && attempt.LockedUntil.Value <= DateTime.UtcNow)
                {
                    _failedAttempts.TryRemove(normalizedEmail, out _);
                }
            }
            return false;
        }

        private void RecordFailedAttempt(string normalizedEmail)
        {
            var attempt = _failedAttempts.AddOrUpdate(
                normalizedEmail,
                new FailedLoginAttempt { Count = 1 },
                (key, existing) =>
                {
                    existing.Count++;
                    if (existing.Count >= MaxFailedAttempts)
                    {
                        existing.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                        _logger.LogWarning("Account {Email} locked due to {Count} failed login attempts", normalizedEmail, existing.Count);
                    }
                    return existing;
                }
            );
        }

        private void ClearFailedAttempts(string normalizedEmail)
        {
            _failedAttempts.TryRemove(normalizedEmail, out _);
        }

        /// <summary>
        /// Checks if the current user is authenticated as an admin.
        /// Checks multiple authentication sources: token cache, session, and cookies.
        /// </summary>
        /// <returns>True if the current user is authenticated; otherwise, false.</returns>
        /// <remarks>
        /// Authentication check order:
        /// 1. Token cache (from cookie/header/session/sessionId/email mapping)
        /// 2. Session storage
        /// 3. Legacy cookie (for backward compatibility)
        /// </remarks>
        public bool IsAuthenticated()
        {
            // SECURITY: Check authentication token cache first (works even when response started)
            // Then fallback to session and cookies
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    // HttpContext is null - cannot authenticate without it (need it to identify user)
                    // This is acceptable in some Blazor Server scenarios where we can't identify the user
                    _logger.LogDebug("IsAuthenticated: HttpContext is null - cannot identify user for authentication check");
                    return false;
                }
                
                _logger.LogWarning("IsAuthenticated: Checking authentication. Session available: {IsAvailable}, Session ID: {SessionId}", 
                    httpContext.Session?.IsAvailable ?? false, httpContext.Session?.Id ?? "null");
                
                // PRIMARY: Check token cache via GetAuthTokenFromRequest (tries cookie/header/session/sessionId/email mapping)
                var token = GetAuthTokenFromRequest();
                if (!string.IsNullOrEmpty(token))
                {
                    if (_authTokens.TryGetValue(token, out var tokenInfo))
                    {
                        // Check if token is expired
                        if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                        {
                            _logger.LogWarning("IsAuthenticated: Token found and valid for user {Email}", tokenInfo.Email);
                            return true;
                        }
                        else
                        {
                            // Token expired - remove it and clean up mappings
                            _authTokens.TryRemove(token, out _);
                            _emailToToken.TryRemove(tokenInfo.Email.ToLower().Trim(), out _);
                            _logger.LogWarning("Authentication token expired for user {Email}, removed from cache", tokenInfo.Email);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("IsAuthenticated: Token found in request but not in cache. Token: {TokenPrefix}...", token.Substring(0, Math.Min(8, token.Length)));
                    }
                }
                else
                {
                    _logger.LogWarning("IsAuthenticated: No token found in request (cookie/header/session/sessionId/email lookup all failed). Total tokens in cache: {TokenCount}, Email mappings: {EmailMappingCount}, Session ID mappings: {SessionMappingCount}", 
                        _authTokens.Count, _emailToToken.Count, _sessionIdToToken.Count);
                    
                    // Debug: Log what's in the cache (only if verbose logging enabled)
                    if (_authTokens.Count > 0 && _logger.IsEnabled(LogLevel.Debug))
                    {
                        foreach (var kvp in _authTokens.Take(3))
                        {
                            _logger.LogDebug("IsAuthenticated: Cache contains token for {Email}, expires at {ExpiresAt}", kvp.Value.Email, kvp.Value.ExpiresAt);
                        }
                    }
                    
                    // Debug: Log session ID mappings
                    if (_sessionIdToToken.Count > 0 && _logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("IsAuthenticated: {MappingCount} session ID mappings exist", _sessionIdToToken.Count);
                    }
                }
                
                // SECONDARY: Check session (preferred if available)
                if (httpContext.Session != null && httpContext.Session.IsAvailable)
                {
                    try
                    {
                        var sessionEmail = httpContext.Session.GetString("AdminEmail");
                        if (!string.IsNullOrEmpty(sessionEmail))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Session read failed - continue to cookie check
                    }
                }
                
                // TERTIARY: Fallback to legacy cookie (for backward compatibility)
                var cookieEmail = httpContext.Request.Cookies["AdminEmail"];
                if (!string.IsNullOrEmpty(cookieEmail))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - authentication check should never crash the page
                _logger.LogWarning(ex, "Failed to check authentication");
            }
            
            return false;
        }

        /// <summary>
        /// Gets the email address of the currently authenticated admin user.
        /// Retrieves email from token cache, session, or cookies.
        /// </summary>
        /// <returns>The email address of the authenticated admin user, or null if no user is authenticated.</returns>
        /// <remarks>
        /// Email retrieval order:
        /// 1. Token cache (from cookie/header/session/sessionId/email mapping)
        /// 2. Session storage
        /// 3. Legacy cookie (for backward compatibility)
        /// </remarks>
        public string? GetCurrentAdminEmail()
        {
            // Get email from token cache first, then session, then cookies
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    return null;
                }
                
                // PRIMARY: Check token cache (works even when response started)
                var token = GetAuthTokenFromRequest();
                if (!string.IsNullOrEmpty(token) && _authTokens.TryGetValue(token, out var tokenInfo))
                {
                    if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                    {
                        return tokenInfo.Email;
                    }
                    else
                    {
                        // Token expired - remove it
                        _authTokens.TryRemove(token, out _);
                    }
                }
                
                // SECONDARY: Check session (preferred if available)
                if (httpContext.Session != null && httpContext.Session.IsAvailable)
                {
                    try
                    {
                        var sessionEmail = httpContext.Session.GetString("AdminEmail");
                        if (!string.IsNullOrEmpty(sessionEmail))
                        {
                            return sessionEmail;
                        }
                    }
                    catch
                    {
                        // Session read failed - continue to cookie check
                    }
                }
                
                // TERTIARY: Fallback to legacy cookie (for backward compatibility)
                var cookieEmail = httpContext.Request.Cookies["AdminEmail"];
                if (!string.IsNullOrEmpty(cookieEmail))
                {
                    return cookieEmail;
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - email retrieval should never crash the page
                _logger.LogWarning(ex, "Failed to get admin email");
            }
            
            return null;
        }

        /// <summary>
        /// Signs out the current admin user and invalidates their session.
        /// Removes authentication tokens from cache, clears session data, and deletes cookies.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SignOutAsync()
        {
            // Clear authentication token, session data, and cookies
            var httpContext = _httpContextAccessor.HttpContext;
            AuthTokenInfo? tokenInfo = null;
            
            // Remove token from cache
            var token = GetAuthTokenFromRequest();
            if (!string.IsNullOrEmpty(token))
            {
                _authTokens.TryRemove(token, out tokenInfo);
                if (tokenInfo != null)
                {
                    ClearFailedAttempts(tokenInfo.Email.ToLower());
                    _logger.LogDebug("Removed authentication token for user {Email} during sign out", tokenInfo.Email);
                }
            }
            
            // Also remove session ID and email mappings
            try
            {
                var sessionId = httpContext?.Session?.Id;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _sessionIdToToken.TryRemove(sessionId, out _);
                    _logger.LogDebug("Removed session ID mapping for session {SessionId} during sign out", sessionId);
                }
            }
            catch
            {
                // Session ID unavailable - that's OK
            }
            
            // Remove email mapping
            if (tokenInfo != null)
            {
                _emailToToken.TryRemove(tokenInfo.Email.ToLower().Trim(), out _);
                _logger.LogDebug("Removed email mapping for {Email} during sign out", tokenInfo.Email);
            }
            else
            {
                // Try to get email from session or cookie to remove email mapping
                try
                {
                    var sessionEmail = httpContext?.Session?.GetString("AdminEmail");
                    if (string.IsNullOrEmpty(sessionEmail))
                    {
                        sessionEmail = httpContext?.Request.Cookies["AdminEmail"];
                    }
                    if (!string.IsNullOrEmpty(sessionEmail))
                    {
                        _emailToToken.TryRemove(sessionEmail.ToLower().Trim(), out _);
                    }
                }
                catch
                {
                    // Email lookup failed - that's OK
                }
            }
            
            // Clear session data
            if (httpContext?.Session != null)
            {
                try
                {
                    var sessionEmail = httpContext.Session.GetString("AdminEmail");
                    if (string.IsNullOrEmpty(sessionEmail))
                    {
                        // Try to get email from token or cookies as fallback
                        if (tokenInfo != null)
                        {
                            sessionEmail = tokenInfo.Email;
                        }
                        else
                        {
                            sessionEmail = httpContext.Request.Cookies["AdminEmail"];
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(sessionEmail))
                    {
                        ClearFailedAttempts(sessionEmail.ToLower());
                    }
                    
                    httpContext.Session.Remove("AdminEmail");
                    httpContext.Session.Remove("AdminLoginTime");
                    httpContext.Session.Remove("AdminAuthToken");
                    await httpContext.Session.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear session during sign out");
                }
            }
            
            // Clear cookies (if response hasn't started)
            if (httpContext?.Response != null && !httpContext.Response.HasStarted)
            {
                try
                {
                    httpContext.Response.Cookies.Delete(AuthTokenCookieName);
                    httpContext.Response.Cookies.Delete("AdminEmail");
                    httpContext.Response.Cookies.Delete("AdminLoginTime");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear cookies during sign out");
                }
            }
            
            await Task.CompletedTask;
        }
    }
}

