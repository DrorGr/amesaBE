using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for handling account deletion and closure operations.
    /// Implements soft delete with grace period for account recovery.
    /// </summary>
    public class AccountDeletionService : IAccountDeletionService
    {
        private const int DefaultGracePeriodDays = 30;

        private readonly AuthDbContext _context;
        private readonly ISessionService _sessionService;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountDeletionService> _logger;
        private readonly int _gracePeriodDays;
        private readonly bool _anonymizeData;

        public AccountDeletionService(
            AuthDbContext context,
            ISessionService sessionService,
            IAuditLogService auditLogService,
            IConfiguration configuration,
            ILogger<AccountDeletionService> logger)
        {
            _context = context;
            _sessionService = sessionService;
            _auditLogService = auditLogService;
            _configuration = configuration;
            _logger = logger;
            _gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", DefaultGracePeriodDays);
            _anonymizeData = _configuration.GetValue<bool>("SecuritySettings:AccountDeletion:AnonymizeData", true);
        }

        public async Task<bool> InitiateAccountDeletionAsync(Guid userId, string password)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Verify password
            if (user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password provided for account deletion by user {UserId}", userId);
                return false;
            }

            // Check if already deleted
            if (user.Status == UserStatus.Deleted)
            {
                _logger.LogWarning("Attempt to delete already deleted account {UserId}", userId);
                return false;
            }

            // Set deletion date (grace period)
            var deletionDate = DateTime.UtcNow.AddDays(_gracePeriodDays);

            // Soft delete: Set status and deletion date
            user.Status = UserStatus.Deleted;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Store deletion date in a custom field or use DeletedAt + grace period
            // For now, we'll use DeletedAt as the initiation date and calculate deletion date

            // Invalidate all sessions
            await _sessionService.InvalidateAllSessionsAsync(userId);

            // Anonymize sensitive data if configured
            if (_anonymizeData)
            {
                await AnonymizeUserDataAsync(user);
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogAuthenticationEventAsync(
                "AccountDeletionInitiated",
                userId,
                true);

            _logger.LogInformation("Account deletion initiated for user {UserId}. Permanent deletion scheduled for {DeletionDate}", userId, deletionDate);

            return true;
        }

        public async Task<bool> CancelAccountDeletionAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if deletion is pending
            if (user.Status != UserStatus.Deleted || user.DeletedAt == null)
            {
                _logger.LogWarning("Attempt to cancel deletion for non-deleted account {UserId}", userId);
                return false;
            }

            // Check if within grace period
            var deletionDate = user.DeletedAt.Value.AddDays(_gracePeriodDays);
            if (DateTime.UtcNow > deletionDate)
            {
                _logger.LogWarning("Attempt to cancel deletion after grace period for user {UserId}", userId);
                return false;
            }

            // Restore account
            user.Status = UserStatus.Active;
            user.DeletedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogAuthenticationEventAsync(
                "AccountDeletionCancelled",
                userId,
                true);

            _logger.LogInformation("Account deletion cancelled for user {UserId}", userId);

            return true;
        }

        public async Task<bool> PermanentlyDeleteAccountAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false; // Already deleted or doesn't exist
            }

            // Check if deletion is pending
            if (user.Status != UserStatus.Deleted || user.DeletedAt == null)
            {
                _logger.LogWarning("Attempt to permanently delete non-deleted account {UserId}", userId);
                return false;
            }

            // Check if grace period has passed
            var deletionDate = user.DeletedAt.Value.AddDays(_gracePeriodDays);
            if (DateTime.UtcNow < deletionDate)
            {
                _logger.LogWarning("Attempt to permanently delete account before grace period ends for user {UserId}", userId);
                return false;
            }

            // Permanently delete: Remove all user data
            // Note: This is a soft delete approach - we keep the record but mark it as deleted
            // For true permanent deletion, you would need to handle cascading deletes
            // For GDPR compliance, we anonymize data instead of hard delete

            // Anonymize all user data
            await AnonymizeUserDataAsync(user);

            // Mark as permanently deleted (could add a separate flag if needed)
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogAuthenticationEventAsync(
                "AccountPermanentlyDeleted",
                userId,
                true);

            _logger.LogInformation("Account permanently deleted for user {UserId}", userId);

            return true;
        }

        public async Task<bool> IsDeletionPendingAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (user.Status != UserStatus.Deleted || user.DeletedAt == null)
            {
                return false;
            }

            // Check if within grace period
            var deletionDate = user.DeletedAt.Value.AddDays(_gracePeriodDays);
            return DateTime.UtcNow <= deletionDate;
        }

        public async Task<DateTime?> GetDeletionDateAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status != UserStatus.Deleted || user.DeletedAt == null)
            {
                return null;
            }

            return user.DeletedAt.Value.AddDays(_gracePeriodDays);
        }

        private async Task AnonymizeUserDataAsync(User user)
        {
            // Anonymize sensitive personal data for GDPR compliance
            // Keep only essential data for audit/legal purposes

            user.Email = $"deleted_{user.Id}@deleted.local";
            user.Username = $"deleted_{user.Id}";
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.Phone = null;
            user.IdNumber = null;
            user.ProfileImageUrl = null;
            user.PasswordHash = null; // Remove password hash
            user.EmailVerificationToken = null;
            user.PhoneVerificationToken = null;
            user.PasswordResetToken = null;
            user.PasswordResetExpiresAt = null;
            user.TwoFactorSecret = null;
            user.ProviderId = null;

            // Note: OAuth tokens are stored in cache (not database), so they will expire naturally
            // No need to clear them here as they're keyed by email hash and will be inaccessible after anonymization

            // Clear security questions
            var securityQuestions = await _context.SecurityQuestions
                .Where(sq => sq.UserId == user.Id)
                .ToListAsync();
            _context.SecurityQuestions.RemoveRange(securityQuestions);

            // Clear backup codes
            var backupCodes = await _context.BackupCodes
                .Where(bc => bc.UserId == user.Id)
                .ToListAsync();
            _context.BackupCodes.RemoveRange(backupCodes);

            _logger.LogInformation("User data anonymized for user {UserId}", user.Id);
        }
    }
}

