using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AmesaBackend.Data;
using BCrypt.Net;
using System.Collections.Concurrent;

namespace AmesaBackend.Services
{
    /// <summary>
    /// Service for admin authentication using email/password
    /// Uses static in-memory storage for Blazor Server compatibility
    /// </summary>
    public class AdminAuthService : IAdminAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AmesaDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, AuthData> _authenticatedUsers = new();
        
        private class AuthData
        {
            public string Email { get; set; } = string.Empty;
            public DateTime LoginTime { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        public AdminAuthService(IHttpContextAccessor httpContextAccessor, AmesaDbContext context, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                Console.WriteLine($"[AdminAuthService] AuthenticateAsync called with email: '{email}', password length: {password?.Length ?? 0}");
                
                // Get admin credentials from environment variables or configuration
                var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? 
                                _configuration["AdminSettings:Email"] ?? 
                                "admin@amesa.com"; // Fallback for development
                
                var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? 
                                   _configuration["AdminSettings:Password"] ?? 
                                   "Admin123!"; // Fallback for development

                Console.WriteLine($"[AdminAuthService] Comparing email: '{email.ToLower()}' == '{adminEmail.ToLower()}' = {email.ToLower() == adminEmail.ToLower()}");
                Console.WriteLine($"[AdminAuthService] Password comparison: {(password == adminPassword ? "MATCH" : "NO MATCH")}");

                if (email.ToLower() == adminEmail.ToLower() && password == adminPassword)
                {
                    Console.WriteLine("[AdminAuthService] Credentials match! Setting authentication in static storage...");
                    
                    // Clean up expired entries
                    CleanupExpiredEntries();
                    
                    // Store authentication state in static storage (2 hours expiration)
                    var expiresAt = DateTime.UtcNow.AddHours(2);
                    var authData = new AuthData
                    {
                        Email = email,
                        LoginTime = DateTime.UtcNow,
                        ExpiresAt = expiresAt
                    };
                    
                    _authenticatedUsers.AddOrUpdate(email.ToLower(), authData, (key, oldValue) => authData);
                    
                    Console.WriteLine($"[AdminAuthService] Authentication set successfully for email: {email}, expires at: {expiresAt}");
                    return true;
                }

                Console.WriteLine("[AdminAuthService] Credentials do not match");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminAuthService] Exception during authentication: {ex}");
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
            // For simplicity, we'll check if any admin is authenticated
            // In a real application, you'd want to track which specific user is authenticated
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
            // For simplicity, we'll clear all authenticated users
            // In a real application, you'd want to track which specific user to sign out
            _authenticatedUsers.Clear();
            await Task.CompletedTask;
        }
    }
}

