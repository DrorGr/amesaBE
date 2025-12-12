using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Auth.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminAuthService> _logger;
        private static readonly ConcurrentDictionary<string, AuthData> _authenticatedUsers = new();
        
        private class AuthData
        {
            public string Email { get; set; } = string.Empty;
            public DateTime LoginTime { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public AdminAuthService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<AdminAuthService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? 
                                _configuration["AdminSettings:Email"] ?? 
                                "admin@amesa.com";
                
                var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? 
                                   _configuration["AdminSettings:Password"] ?? 
                                   "Admin123!";

                _logger.LogDebug("Login attempt for email: {Email}, Expected email: {AdminEmail}", email, adminEmail);
                _logger.LogDebug("Password provided: {HasPassword}, Expected password length: {PasswordLength}", !string.IsNullOrEmpty(password), adminPassword?.Length ?? 0);

                // Compare email (case-insensitive) and password (case-sensitive)
                var emailMatch = email.ToLower().Trim() == adminEmail.ToLower().Trim();
                var passwordMatch = password == adminPassword;

                _logger.LogDebug("Email match: {EmailMatch}, Password match: {PasswordMatch}", emailMatch, passwordMatch);

                if (emailMatch && passwordMatch)
                {
                    CleanupExpiredEntries();
                    
                    var expiresAt = DateTime.UtcNow.AddHours(2);
                    var authData = new AuthData
                    {
                        Email = email,
                        LoginTime = DateTime.UtcNow,
                        ExpiresAt = expiresAt
                    };
                    
                    _authenticatedUsers.AddOrUpdate(email.ToLower(), authData, (key, oldValue) => authData);
                    _logger.LogInformation("User {Email} authenticated successfully", email);
                    
                    // Store email in session for current user tracking
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext?.Session != null)
                    {
                        try
                        {
                            // Ensure session is available
                            await httpContext.Session.LoadAsync();
                            httpContext.Session.SetString("AdminEmail", email);
                            await httpContext.Session.CommitAsync();
                            _logger.LogDebug("Session stored for user {Email}", email);
                        }
                        catch (Exception sessionEx)
                        {
                            _logger.LogWarning(sessionEx, "Failed to store session for user {Email}, but authentication succeeded", email);
                            // Continue even if session fails - the in-memory dictionary will work
                        }
                    }
                    else
                    {
                        _logger.LogWarning("HttpContext or Session is null for user {Email}", email);
                    }
                    
                    return true;
                }

                _logger.LogWarning("Authentication failed for email: {Email} - Email match: {EmailMatch}, Password match: {PasswordMatch}", email, emailMatch, passwordMatch);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during authentication for email: {Email}", email);
                return false;
            }
        }
        
        private static void CleanupExpiredEntries()
        {
            var expiredKeys = _authenticatedUsers
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var key in expiredKeys)
            {
                _authenticatedUsers.TryRemove(key, out _);
            }
        }

        public bool IsAuthenticated()
        {
            CleanupExpiredEntries();
            
            // Check session for authenticated user
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                var sessionEmail = httpContext.Session.GetString("AdminEmail");
                if (!string.IsNullOrEmpty(sessionEmail))
                {
                    // Verify the session email is still in authenticated users
                    if (_authenticatedUsers.TryGetValue(sessionEmail.ToLower(), out var authData) && 
                        authData.ExpiresAt > DateTime.UtcNow)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public string? GetCurrentAdminEmail()
        {
            CleanupExpiredEntries();
            
            // Get email from session
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                var sessionEmail = httpContext.Session.GetString("AdminEmail");
                if (!string.IsNullOrEmpty(sessionEmail))
                {
                    // Verify the session email is still valid
                    if (_authenticatedUsers.TryGetValue(sessionEmail.ToLower(), out var authData) && 
                        authData.ExpiresAt > DateTime.UtcNow)
                    {
                        return sessionEmail;
                    }
                }
            }
            
            return null;
        }

        public async Task SignOutAsync()
        {
            // Remove current user from authenticated users
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                var sessionEmail = httpContext.Session.GetString("AdminEmail");
                if (!string.IsNullOrEmpty(sessionEmail))
                {
                    _authenticatedUsers.TryRemove(sessionEmail.ToLower(), out _);
                }
                httpContext.Session.Remove("AdminEmail");
            }
            
            await Task.CompletedTask;
        }
    }
}

