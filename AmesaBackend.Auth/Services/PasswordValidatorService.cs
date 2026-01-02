using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
using BCrypt.Net;
using System.Text.RegularExpressions;

namespace AmesaBackend.Auth.Services
{
    public class PasswordValidatorService : IPasswordValidatorService
    {
        private const int DefaultCheckLastPasswords = 5;

        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordValidatorService> _logger;
        private readonly IPasswordBreachService? _breachService;
        private readonly int _checkLastPasswords;
        private readonly bool _blockBreachedPasswords;

        public PasswordValidatorService(
            AuthDbContext context,
            IConfiguration configuration,
            ILogger<PasswordValidatorService> logger,
            IPasswordBreachService? breachService = null)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _breachService = breachService; // Optional - service may not be registered
            _checkLastPasswords = _configuration.GetValue<int>("SecuritySettings:PasswordHistory:CheckLastPasswords", DefaultCheckLastPasswords);
            _blockBreachedPasswords = _configuration.GetValue<bool>("SecuritySettings:PasswordBreachCheck:BlockBreachedPasswords", false);
        }
        // Expanded list of 100+ most common weak passwords
        // Based on Have I Been Pwned, NordPass, and other security research
        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Top 20 most common
            "password", "12345678", "123456789", "1234567890", "qwerty", "abc123", "password123",
            "admin", "letmein", "welcome", "monkey", "password1", "123456", "1234567",
            "12345", "1234", "123", "qwerty123", "password12", "admin123",
            
            // Common patterns and variations
            "password2", "password3", "password4", "password5", "password6", "password7",
            "password8", "password9", "password0", "pass1234", "pass123", "passw0rd",
            "p@ssw0rd", "P@ssw0rd", "P@$$w0rd", "Passw0rd", "PASSWORD", "Password",
            
            // Sequential numbers
            "12345678901", "123456789012", "987654321", "87654321", "7654321", "654321",
            "11111111", "22222222", "33333333", "44444444", "55555555", "66666666",
            "77777777", "88888888", "99999999", "00000000", "123123123", "321321321",
            
            // Keyboard patterns
            "qwertyuiop", "qwertyui", "qwerty1", "qwerty12", "qwerty123", "qwertyuiop123",
            "asdfghjkl", "asdfgh", "asdf123", "zxcvbnm", "zxcvbn", "1qaz2wsx", "qazwsx",
            
            // Common words and phrases
            "welcome123", "welcome1", "welcome12", "letmein1", "letmein123", "master",
            "master123", "root", "root123", "toor", "toor123", "test", "test123",
            "test1234", "testing", "testing123", "demo", "demo123", "guest", "guest123",
            
            // Common names and combinations
            "michael", "michael123", "jennifer", "jennifer123", "joshua", "joshua123",
            "daniel", "daniel123", "matthew", "matthew123", "david", "david123",
            "james", "james123", "robert", "robert123", "john", "john123", "joe", "joe123",
            
            // Sports and hobbies
            "football", "football123", "baseball", "baseball123", "basketball", "basketball123",
            "soccer", "soccer123", "hockey", "hockey123", "tennis", "tennis123",
            
            // Technology terms
            "computer", "computer123", "internet", "internet123", "software", "software123",
            "hardware", "hardware123", "windows", "windows123", "microsoft", "microsoft123",
            
            // Simple patterns
            "abc123", "abc1234", "abcd1234", "abcde123", "abcdef123", "a1b2c3", "a1b2c3d4",
            "1a2b3c", "1a2b3c4d", "aa123456", "aaa123456", "aaaa1234", "aaaaa123",
            
            // Company/product names
            "company", "company123", "business", "business123", "work", "work123",
            "office", "office123", "email", "email123", "user", "user123", "users",
            
            // Common phrases
            "iloveyou", "iloveyou123", "love", "love123", "hello", "hello123", "hi123",
            "goodbye", "goodbye123", "thanks", "thanks123", "please", "please123",
            
            // Additional weak passwords
            "superman", "superman123", "batman", "batman123", "spiderman", "spiderman123",
            "princess", "princess123", "dragon", "dragon123", "shadow", "shadow123",
            "sunshine", "sunshine123", "charlie", "charlie123", "samantha", "samantha123"
        };


        public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, Guid? userId = null)
        {
            var result = new PasswordValidationResult { IsValid = true };

            // Length check (early exit on failure)
            if (password.Length < 8 || password.Length > 128)
            {
                result.IsValid = false;
                result.Errors.Add("Password must be between 8 and 128 characters");
                return result; // Early exit - no need to check further
            }

            // Character requirements (early exit on first failure)
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter");
                return result; // Early exit
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter");
                return result; // Early exit
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one digit");
                return result; // Early exit
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character");
                return result; // Early exit
            }

            // Common password check (early exit on failure)
            if (CommonPasswords.Contains(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is too common. Please choose a more unique password");
                return result; // Early exit - no need to check history
            }

            // Check password history if userId provided (only if all other checks passed)
            if (userId.HasValue && await IsPasswordInHistoryAsync(password, userId.Value))
            {
                result.IsValid = false;
                result.Errors.Add("Password was recently used. Please choose a different password");
                return result; // Early exit
            }

            // Check password breach status (only if all other checks passed and service is available)
            if (_breachService != null)
            {
                try
                {
                    var isBreached = await _breachService.IsPasswordBreachedAsync(password);
                    if (isBreached)
                    {
                        var breachCount = await _breachService.GetBreachCountAsync(password);
                        var message = breachCount > 0
                            ? $"This password has been found in {breachCount} data breach{(breachCount > 1 ? "es" : "")}. Please choose a different password."
                            : "This password has been found in data breaches. Please choose a different password.";

                        if (_blockBreachedPasswords)
                        {
                            result.IsValid = false;
                            result.Errors.Add(message);
                            return result; // Early exit - block breached passwords
                        }
                        else
                        {
                            // Warn but don't block
                            result.Errors.Add(message);
                            result.Warnings = result.Warnings ?? new List<string>();
                            result.Warnings.Add(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Fail open - don't block user if breach check fails
                    _logger.LogError(ex, "Error checking password breach status");
                }
            }

            // Calculate strength (only if all validations passed)
            result.Strength = CalculateStrength(password);

            return result;
        }

        public async Task<bool> IsPasswordInHistoryAsync(string password, Guid userId)
        {
            try
            {
                // Optimize: Only check last N passwords (most recent, configurable)
                // BCrypt.Verify is expensive, so we limit the check
                var history = await _context.Set<UserPasswordHistory>()
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(_checkLastPasswords)
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

