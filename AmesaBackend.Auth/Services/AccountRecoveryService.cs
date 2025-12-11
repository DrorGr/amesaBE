using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for account recovery operations.
    /// </summary>
    public class AccountRecoveryService : IAccountRecoveryService
    {
        private readonly AuthDbContext _context;
        private readonly Lazy<IPasswordResetService> _passwordResetService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountRecoveryService> _logger;

        public AccountRecoveryService(
            AuthDbContext context,
            Lazy<IPasswordResetService> passwordResetService,
            IEmailVerificationService emailVerificationService,
            ITokenService tokenService,
            IConfiguration configuration,
            ILogger<AccountRecoveryService> logger)
        {
            _context = context;
            _passwordResetService = passwordResetService;
            _emailVerificationService = emailVerificationService;
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> InitiateEmailRecoveryAsync(string email)
        {
            var user = await _context.Users
                .IgnoreQueryFilters() // Allow soft-deleted users during grace period
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Don't reveal if user exists (prevent enumeration)
                return true; // Always return success
            }

            // Check if account is soft-deleted and grace period expired
            if (user.DeletedAt != null)
            {
                var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                if (user.DeletedAt.Value.AddDays(gracePeriodDays) < DateTime.UtcNow)
                {
                    // Grace period expired - don't allow recovery
                    return true; // Still return success to prevent enumeration
                }
            }

            // Check if account is locked (log but still allow recovery)
            if (user.LockedUntil != null && user.LockedUntil > DateTime.UtcNow)
            {
                _logger.LogWarning("Recovery initiated for locked account: {UserId}", user.Id);
            }

            // Invalidate phone recovery if it exists (prevent concurrent recovery)
            if (!string.IsNullOrEmpty(user.PhoneVerificationToken))
            {
                user.PhoneVerificationToken = null;
            }

            // Use existing password reset service for email recovery
            var request = new DTOs.ForgotPasswordRequest { Email = email };
            await _passwordResetService.Value.ForgotPasswordAsync(request);

            _logger.LogInformation("Email recovery initiated for user: {UserId}", user.Id);
            return true;
        }

        public async Task<bool> InitiatePhoneRecoveryAsync(string phone)
        {
            var user = await _context.Users
                .IgnoreQueryFilters() // Allow soft-deleted users during grace period
                .FirstOrDefaultAsync(u => u.Phone == phone);
            if (user == null)
            {
                // Don't reveal if user exists (prevent enumeration)
                return true; // Always return success
            }

            // Check if account is soft-deleted and grace period expired
            if (user.DeletedAt != null)
            {
                var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                if (user.DeletedAt.Value.AddDays(gracePeriodDays) < DateTime.UtcNow)
                {
                    // Grace period expired - don't allow recovery
                    return true; // Still return success to prevent enumeration
                }
            }

            // Check if account is locked (log but still allow recovery)
            if (user.LockedUntil != null && user.LockedUntil > DateTime.UtcNow)
            {
                _logger.LogWarning("Recovery initiated for locked account: {UserId}", user.Id);
            }

            // Invalidate email recovery if it exists (prevent concurrent recovery)
            if (!string.IsNullOrEmpty(user.PasswordResetToken))
            {
                user.PasswordResetToken = null;
                user.PasswordResetExpiresAt = null;
            }

            // Generate recovery code (6-digit)
            var code = GenerateRecoveryCode();
            var codeHash = BCrypt.Net.BCrypt.HashPassword(code);

            // Store in user's phone verification token (reuse existing field)
            user.PhoneVerificationToken = codeHash;
            await _context.SaveChangesAsync();

            // TODO: Send SMS via notification service
            // For now, log the code (remove in production)
            _logger.LogInformation("Phone recovery code for user {UserId}: {Code}", user.Id, code);
            // Note: In production, send SMS via notification service

            _logger.LogInformation("Phone recovery initiated for user: {UserId}", user.Id);
            return true;
        }

        public async Task<bool> VerifySecurityQuestionAsync(Guid userId, string question, string answer)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return false;
            }

            // Check if account is soft-deleted and grace period expired
            if (user.DeletedAt != null)
            {
                var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                if (user.DeletedAt.Value.AddDays(gracePeriodDays) < DateTime.UtcNow)
                {
                    return false; // Grace period expired
                }
            }

            // Check if account is suspended or banned
            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            {
                return false;
            }

            var securityQuestion = await _context.SecurityQuestions
                .FirstOrDefaultAsync(sq => sq.UserId == userId && sq.Question == question);

            if (securityQuestion == null)
            {
                return false;
            }

            var isValid = BCrypt.Net.BCrypt.Verify(answer, securityQuestion.AnswerHash);
            
            if (isValid)
            {
                _logger.LogInformation("Security question verified for user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid security question answer for user: {UserId}", userId);
            }

            return isValid;
        }

        public async Task<Services.RecoveryMethodsResponse> GetRecoveryMethodsAsync(string identifier)
        {
            // Try to find user by email or phone (with IgnoreQueryFilters to allow soft-deleted users during grace period)
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == identifier || u.Phone == identifier);

            if (user == null)
            {
                // Don't reveal if user exists - return empty methods
                return new Services.RecoveryMethodsResponse();
            }

            var hasSecurityQuestions = await _context.SecurityQuestions
                .AnyAsync(sq => sq.UserId == user.Id);

            return new Services.RecoveryMethodsResponse
            {
                HasEmail = !string.IsNullOrEmpty(user.Email),
                HasPhone = !string.IsNullOrEmpty(user.Phone) && user.PhoneVerified,
                HasSecurityQuestions = hasSecurityQuestions,
                MaskedEmail = MaskEmail(user.Email),
                MaskedPhone = MaskPhone(user.Phone)
            };
        }

        public async Task SetupSecurityQuestionsAsync(Guid userId, List<Services.SecurityQuestionRequest> questions)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Delete existing security questions
            var existingQuestions = await _context.SecurityQuestions
                .Where(sq => sq.UserId == userId)
                .ToListAsync();
            _context.SecurityQuestions.RemoveRange(existingQuestions);

            // Add new security questions
            var securityQuestions = questions.Select(q => new SecurityQuestion
            {
                UserId = userId,
                Question = q.Question,
                AnswerHash = BCrypt.Net.BCrypt.HashPassword(q.Answer),
                Order = q.Order,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _context.SecurityQuestions.AddRange(securityQuestions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Security questions set up for user: {UserId} ({Count} questions)", userId, questions.Count);
        }

        public async Task<bool> VerifyRecoveryCodeAsync(string identifier, string code, RecoveryMethod method)
        {
            User? user = null;

            if (method == RecoveryMethod.Email)
            {
                user = await _context.Users
                    .IgnoreQueryFilters() // Allow soft-deleted users during grace period
                    .FirstOrDefaultAsync(u => u.Email == identifier);
                if (user == null) return false;

                // Check if account is soft-deleted and grace period expired
                if (user.DeletedAt != null)
                {
                    var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                    if (user.DeletedAt.Value.AddDays(gracePeriodDays) < DateTime.UtcNow)
                    {
                        return false; // Grace period expired
                    }
                }

                // Verify using password reset token (reuse existing flow)
                if (user.PasswordResetToken != null && 
                    user.PasswordResetExpiresAt != null &&
                    user.PasswordResetExpiresAt > DateTime.UtcNow &&
                    BCrypt.Net.BCrypt.Verify(code, user.PasswordResetToken))
                {
                    // Don't clear token here - it will be cleared in ResetPasswordAsync after successful password reset
                    // This prevents token reuse while allowing password reset to complete
                    return true;
                }
            }
            else if (method == RecoveryMethod.Phone)
            {
                user = await _context.Users
                    .IgnoreQueryFilters() // Allow soft-deleted users during grace period
                    .FirstOrDefaultAsync(u => u.Phone == identifier);
                if (user == null) return false;

                // Check if account is soft-deleted and grace period expired
                if (user.DeletedAt != null)
                {
                    var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                    if (user.DeletedAt.Value.AddDays(gracePeriodDays) < DateTime.UtcNow)
                    {
                        return false; // Grace period expired
                    }
                }

                // Verify using phone verification token
                if (user.PhoneVerificationToken != null && BCrypt.Net.BCrypt.Verify(code, user.PhoneVerificationToken))
                {
                    // Don't clear token here - it will be cleared in ResetPasswordAsync after successful password reset
                    // This prevents token reuse while allowing password reset to complete
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a 6-digit recovery code.
        /// </summary>
        private string GenerateRecoveryCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Masks email address for display (e.g., u***@example.com).
        /// </summary>
        private string? MaskEmail(string? email)
        {
            if (string.IsNullOrEmpty(email)) return null;

            var parts = email.Split('@');
            if (parts.Length != 2) return email;

            var username = parts[0];
            var domain = parts[1];

            if (username.Length <= 2)
            {
                return $"{username[0]}***@{domain}";
            }

            return $"{username[0]}***@{domain}";
        }

        /// <summary>
        /// Masks phone number for display (e.g., +1***1234).
        /// </summary>
        private string? MaskPhone(string? phone)
        {
            if (string.IsNullOrEmpty(phone)) return null;

            if (phone.Length <= 4)
            {
                return "***";
            }

            return $"{phone.Substring(0, 2)}***{phone.Substring(phone.Length - 4)}";
        }
    }
}

