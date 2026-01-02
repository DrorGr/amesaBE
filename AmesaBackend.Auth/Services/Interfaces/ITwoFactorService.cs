using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.Services.Interfaces
{
    /// <summary>
    /// Service for two-factor authentication (2FA) operations.
    /// </summary>
    public interface ITwoFactorService
    {
        /// <summary>
        /// Generates a new TOTP secret for a user and returns the setup information.
        /// </summary>
        Task<(string Secret, string QrCodeImageUrl, string ManualEntryKey)> GenerateSetupAsync(Guid userId, string email);

        /// <summary>
        /// Verifies a TOTP code during 2FA setup.
        /// </summary>
        Task<bool> VerifySetupCodeAsync(Guid userId, string code);

        /// <summary>
        /// Enables 2FA for a user after successful setup verification.
        /// </summary>
        Task EnableTwoFactorAsync(Guid userId);

        /// <summary>
        /// Verifies a TOTP code during login.
        /// </summary>
        Task<bool> VerifyCodeAsync(Guid userId, string code);

        /// <summary>
        /// Verifies a backup code during login.
        /// </summary>
        Task<bool> VerifyBackupCodeAsync(Guid userId, string code);

        /// <summary>
        /// Generates new backup codes for a user (invalidates old ones).
        /// </summary>
        Task<List<string>> GenerateBackupCodesAsync(Guid userId);

        /// <summary>
        /// Disables 2FA for a user.
        /// </summary>
        Task DisableTwoFactorAsync(Guid userId);

        /// <summary>
        /// Checks if 2FA is enabled for a user.
        /// </summary>
        Task<bool> IsTwoFactorEnabledAsync(Guid userId);
    }
}

