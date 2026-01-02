using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for two-factor authentication (2FA) operations using TOTP.
    /// </summary>
    public class TwoFactorService : ITwoFactorService
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwoFactorService> _logger;

        public TwoFactorService(
            AuthDbContext context,
            IConfiguration configuration,
            ILogger<TwoFactorService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(string Secret, string QrCodeImageUrl, string ManualEntryKey)> GenerateSetupAsync(Guid userId, string email)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Generate a new TOTP secret (32 bytes = 256 bits)
            var secretBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secretBytes);
            }

            var secret = Base32Encoding.ToString(secretBytes);
            
            // Store the secret temporarily (will be saved after verification)
            user.TwoFactorSecret = secret;
            await _context.SaveChangesAsync();

            // Generate QR code
            var issuer = _configuration["SecuritySettings:TwoFactorIssuer"] ?? "Amesa";
            var accountName = email;
            var manualEntryKey = secret; // For manual entry
            var otpAuthUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

            // Generate QR code as base64 image
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
            var qrCodeImageUrl = $"data:image/png;base64,{qrCodeBase64}";

            return (secret, qrCodeImageUrl, manualEntryKey);
        }

        public async Task<bool> VerifySetupCodeAsync(Guid userId, string code)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return false;
            }

            // Verify the code
            var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
            var totp = new Totp(secretBytes);
            var isValid = totp.VerifyTotp(code, out _, new VerificationWindow(1, 1)); // Allow 1 step before/after

            if (isValid)
            {
                _logger.LogInformation("2FA setup code verified for user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid 2FA setup code for user: {UserId}", userId);
            }

            return isValid;
        }

        public async Task EnableTwoFactorAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                throw new InvalidOperationException("2FA secret not generated. Call GenerateSetupAsync first.");
            }

            // Enable 2FA
            user.TwoFactorEnabled = true;
            
            // Generate backup codes
            await GenerateBackupCodesAsync(userId);

            await _context.SaveChangesAsync();
            _logger.LogInformation("2FA enabled for user: {UserId}", userId);
        }

        public async Task<bool> VerifyCodeAsync(Guid userId, string code)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return false;
            }

            // Verify TOTP code
            var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
            var totp = new Totp(secretBytes);
            var isValid = totp.VerifyTotp(code, out _, new VerificationWindow(1, 1)); // Allow 1 step before/after

            if (isValid)
            {
                _logger.LogInformation("2FA code verified for user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid 2FA code for user: {UserId}", userId);
            }

            return isValid;
        }

        public async Task<bool> VerifyBackupCodeAsync(Guid userId, string code)
        {
            // Get all unused backup codes for the user
            var backupCodes = await _context.BackupCodes
                .Where(bc => bc.UserId == userId && !bc.IsUsed)
                .ToListAsync();

            foreach (var backupCode in backupCodes)
            {
                // Verify using BCrypt (same as password verification)
                if (BCrypt.Net.BCrypt.Verify(code, backupCode.CodeHash))
                {
                    // Mark as used
                    backupCode.IsUsed = true;
                    backupCode.UsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Backup code used for user: {UserId}", userId);
                    return true;
                }
            }

            _logger.LogWarning("Invalid backup code for user: {UserId}", userId);
            return false;
        }

        public async Task<List<string>> GenerateBackupCodesAsync(Guid userId)
        {
            // Delete old backup codes
            var oldCodes = await _context.BackupCodes
                .Where(bc => bc.UserId == userId)
                .ToListAsync();
            _context.BackupCodes.RemoveRange(oldCodes);

            // Generate 10 new backup codes
            var codes = new List<string>();
            var backupCodes = new List<BackupCode>();

            for (int i = 0; i < 10; i++)
            {
                // Generate 8-character alphanumeric code
                var code = GenerateBackupCode();
                codes.Add(code);

                // Hash the code
                var codeHash = BCrypt.Net.BCrypt.HashPassword(code);

                backupCodes.Add(new BackupCode
                {
                    UserId = userId,
                    CodeHash = codeHash,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.BackupCodes.AddRange(backupCodes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated 10 backup codes for user: {UserId}", userId);
            return codes;
        }

        public async Task DisableTwoFactorAsync(Guid userId)
        {
            var user = await _context.Users
                .IgnoreQueryFilters() // Allow checking soft-deleted users
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if account is soft-deleted
            if (user.DeletedAt != null)
            {
                throw new InvalidOperationException("Cannot disable 2FA for deleted account");
            }

            // Check if account is suspended or banned
            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            {
                throw new InvalidOperationException("Cannot disable 2FA for suspended or banned account");
            }

            // Disable 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;

            // Delete all backup codes
            var backupCodes = await _context.BackupCodes
                .Where(bc => bc.UserId == userId)
                .ToListAsync();
            _context.BackupCodes.RemoveRange(backupCodes);

            await _context.SaveChangesAsync();
            _logger.LogInformation("2FA disabled for user: {UserId}", userId);
        }

        public async Task<bool> IsTwoFactorEnabledAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user != null && user.TwoFactorEnabled;
        }

        /// <summary>
        /// Generates a random 8-character alphanumeric backup code.
        /// </summary>
        private string GenerateBackupCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

