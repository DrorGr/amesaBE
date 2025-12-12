using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace AmesaBackend.Auth.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, AuthData> _authenticatedUsers = new();
        
        private class AuthData
        {
            public string Email { get; set; } = string.Empty;
            public DateTime LoginTime { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public AdminAuthService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
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

                if (email.ToLower() == adminEmail.ToLower() && password == adminPassword)
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
                    
                    // Store email in session for current user tracking
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext?.Session != null)
                    {
                        httpContext.Session.SetString("AdminEmail", email);
                    }
                    
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
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

