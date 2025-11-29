using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using BCrypt.Net;
using System.Text.RegularExpressions;

namespace AmesaBackend.Auth.Services
{
    public class PasswordValidatorService : IPasswordValidatorService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<PasswordValidatorService> _logger;
        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "12345678", "123456789", "qwerty", "abc123", "password123",
            "admin", "letmein", "welcome", "monkey", "1234567890", "password1"
        };

        public PasswordValidatorService(
            AuthDbContext context,
            ILogger<PasswordValidatorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, Guid? userId = null)
        {
            var result = new PasswordValidationResult { IsValid = true };

            // Length check
            if (password.Length < 8 || password.Length > 128)
            {
                result.IsValid = false;
                result.Errors.Add("Password must be between 8 and 128 characters");
            }

            // Character requirements
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one digit");
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character");
            }

            // Common password check
            if (CommonPasswords.Contains(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is too common. Please choose a more unique password");
            }

            // Check password history if userId provided
            if (userId.HasValue && await IsPasswordInHistoryAsync(password, userId.Value))
            {
                result.IsValid = false;
                result.Errors.Add("Password was recently used. Please choose a different password");
            }

            // Calculate strength
            if (result.IsValid)
            {
                result.Strength = CalculateStrength(password);
            }

            return result;
        }

        public async Task<bool> IsPasswordInHistoryAsync(string password, Guid userId)
        {
            try
            {
                // Optimize: Only check last 5 passwords (most recent)
                // BCrypt.Verify is expensive, so we limit the check
                var history = await _context.Set<UserPasswordHistory>()
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(5)
                    .Select(h => h.PasswordHash) // Only select hash, not full entity
                    .ToListAsync();

                // Early exit optimization: Check in parallel for better performance
                var tasks = history.Select(hash => Task.Run(() => 
                {
                    try
                    {
                        return BCrypt.Net.BCrypt.Verify(password, hash);
                    }
                    catch
                    {
                        return false; // Invalid hash format
                    }
                }));

                var results = await Task.WhenAll(tasks);
                return results.Any(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password history for user: {UserId}", userId);
                return false; // Fail open
            }
        }

        private PasswordStrength CalculateStrength(string password)
        {
            int score = 0;

            // Length bonus
            if (password.Length >= 12) score += 2;
            else if (password.Length >= 8) score += 1;

            // Character variety
            if (Regex.IsMatch(password, @"[A-Z]")) score += 1;
            if (Regex.IsMatch(password, @"[a-z]")) score += 1;
            if (Regex.IsMatch(password, @"[0-9]")) score += 1;
            if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]")) score += 1;

            // Additional complexity
            if (password.Length > 16) score += 1;
            if (Regex.Matches(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]").Count > 2) score += 1;

            return score switch
            {
                >= 7 => PasswordStrength.VeryStrong,
                >= 5 => PasswordStrength.Strong,
                >= 3 => PasswordStrength.Medium,
                _ => PasswordStrength.Weak
            };
        }
    }
}

