using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Auth.Models
{
    /// <summary>
    /// Represents a backup code for two-factor authentication.
    /// Backup codes are single-use codes that can be used instead of TOTP codes.
    /// </summary>
    public class BackupCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Hashed backup code (using BCrypt)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CodeHash { get; set; } = string.Empty;

        /// <summary>
        /// Whether this backup code has been used
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// When this backup code was used (null if not used)
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// When this backup code was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }
}



