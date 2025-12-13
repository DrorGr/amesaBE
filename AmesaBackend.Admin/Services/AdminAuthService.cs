using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Models;
using BCrypt.Net;
using AmesaBackend.Auth.Services;
using System.Collections.Concurrent;

namespace AmesaBackend.Admin.Services
{
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
        
        private class FailedLoginAttempt
        {
            public int Count { get; set; }
            public DateTime? LockedUntil { get; set; }
        }

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

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                _logger.LogDebug("Login attempt for email: {Email}", email);

                // Check for account lockout
                var normalizedEmail = email.ToLower().Trim();
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
                if (_adminDbContext != null)
                {
                    try
                    {
                        _logger.LogDebug("Querying database for admin user: {Email} (normalized: {NormalizedEmail})", email, normalizedEmail);
                        
                        // Use parameterized raw SQL query to avoid EF Core schema/alias translation issues
                        // Query with explicit schema name and case-insensitive comparison
                        // Use FormattableString for proper parameterization
                        // Note: EF Core will handle connection management automatically
                        FormattableString sql = $@"
                            SELECT id, email, username, password_hash, is_active, created_at, last_login_at 
                            FROM amesa_admin.admin_users 
                            WHERE LOWER(TRIM(email)) = {normalizedEmail} 
                            AND is_active = true 
                            LIMIT 1";
                        
                        adminUser = await _adminDbContext.AdminUsers
                            .FromSqlInterpolated(sql)
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
                        _logger.LogError(dbEx, "Database error while querying admin user {Email}. Exception type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}", 
                            email, dbEx.GetType().Name, dbEx.Message, dbEx.InnerException?.Message ?? "None");
                        // Continue to legacy auth fallback only if explicitly configured
                    }
                }
                else
                {
                    _logger.LogWarning("AdminDbContext is null - database authentication unavailable for email: {Email}. Check DB_CONNECTION_STRING environment variable.", email);
                }

                bool authenticated = false;

                if (adminUser != null)
                {
                    // Verify password using BCrypt
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
                                    // Use raw SQL to update last_login_at to avoid tracking issues
                                    FormattableString updateSql = $@"
                                        UPDATE amesa_admin.admin_users 
                                        SET last_login_at = {DateTime.UtcNow} 
                                        WHERE id = {adminUser.Id}";
                                    
                                    await _adminDbContext.Database.ExecuteSqlInterpolatedAsync(updateSql);
                                    _logger.LogDebug("Updated last login time for user {Email}", email);
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
                    // Fallback to legacy config-based authentication ONLY if explicitly configured
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
                        _logger.LogWarning("Authentication failed for email: {Email} - User not found and legacy auth not configured", email);
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
            // Store email in session for authentication tracking
            // SECURITY: Rely solely on session storage (Redis in production) for multi-instance support
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                try
                {
                    // Ensure session is available
                    if (!httpContext.Session.IsAvailable)
                    {
                        await httpContext.Session.LoadAsync();
                    }
                    
                    if (!httpContext.Session.IsAvailable)
                    {
                        _logger.LogError("Session is not available after LoadAsync() for user {Email}. Session may not be configured properly.", email);
                        return false;
                    }
                    
                    httpContext.Session.SetString("AdminEmail", email);
                    httpContext.Session.SetString("AdminLoginTime", DateTime.UtcNow.ToString("O"));
                    await httpContext.Session.CommitAsync();
                    
                    // Verify session was stored
                    var storedEmail = httpContext.Session.GetString("AdminEmail");
                    if (storedEmail != email)
                    {
                        _logger.LogError("Session verification failed for user {Email}. Stored email: {StoredEmail}", email, storedEmail);
                        return false;
                    }
                    
                    _logger.LogInformation("Session successfully stored and verified for user {Email}", email);
                }
                catch (Exception sessionEx)
                {
                    _logger.LogError(sessionEx, "Failed to store session for user {Email}. Exception: {Message}, StackTrace: {StackTrace}", 
                        email, sessionEx.Message, sessionEx.StackTrace);
                    // Session failure is critical - authentication cannot proceed without session
                    return false;
                }
            }
            else
            {
                _logger.LogError("HttpContext or Session is null for user {Email}. HttpContext: {HasContext}, Session: {HasSession}", 
                    email, httpContext != null, httpContext?.Session != null);
                return false;
            }
            
            return true;
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

        public bool IsAuthenticated()
        {
            // SECURITY: Rely solely on session storage for authentication state
            // Session timeout is handled by ASP.NET Core session middleware
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Session != null)
                {
                    // Check if session is available (may not be loaded yet)
                    if (httpContext.Session.IsAvailable)
                    {
                        var sessionEmail = httpContext.Session.GetString("AdminEmail");
                        if (!string.IsNullOrEmpty(sessionEmail))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - authentication check should never crash the page
                _logger.LogWarning(ex, "Failed to check session authentication");
            }
            
            return false;
        }

        public string? GetCurrentAdminEmail()
        {
            // Get email from session
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Session != null && httpContext.Session.IsAvailable)
                {
                    return httpContext.Session.GetString("AdminEmail");
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - email retrieval should never crash the page
                _logger.LogWarning(ex, "Failed to get session email");
            }
            
            return null;
        }

        public async Task SignOutAsync()
        {
            // Clear session data
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                try
                {
                    var sessionEmail = httpContext.Session.GetString("AdminEmail");
                    if (!string.IsNullOrEmpty(sessionEmail))
                    {
                        ClearFailedAttempts(sessionEmail.ToLower());
                    }
                    httpContext.Session.Remove("AdminEmail");
                    httpContext.Session.Remove("AdminLoginTime");
                    await httpContext.Session.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear session during sign out");
                }
            }
            
            await Task.CompletedTask;
        }
    }
}

