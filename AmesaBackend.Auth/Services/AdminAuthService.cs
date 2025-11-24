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
                    return true;
                }

                return false;
            }
            catch (Exception)
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
            return _authenticatedUsers.Any(kvp => kvp.Value.ExpiresAt > DateTime.UtcNow);
        }

        public string? GetCurrentAdminEmail()
        {
            CleanupExpiredEntries();
            var validAuth = _authenticatedUsers.FirstOrDefault(kvp => kvp.Value.ExpiresAt > DateTime.UtcNow);
            return validAuth.Value?.Email;
        }

        public async Task SignOutAsync()
        {
            _authenticatedUsers.Clear();
            await Task.CompletedTask;
        }
    }
}

