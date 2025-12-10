using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Auth.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public bool EmailVerified { get; set; } = false;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool PhoneVerified { get; set; } = false;

        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public GenderType? Gender { get; set; }

        [MaxLength(50)]
        public string? IdNumber { get; set; }

        public UserStatus Status { get; set; } = UserStatus.Pending;

        public UserVerificationStatus VerificationStatus { get; set; } = UserVerificationStatus.Unverified;

        public AuthProvider AuthProvider { get; set; } = AuthProvider.Email;

        [MaxLength(255)]
        public string? ProviderId { get; set; }

        public string? ProfileImageUrl { get; set; }

        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        [MaxLength(50)]
        public string Timezone { get; set; } = "UTC";

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(255)]
        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiresAt { get; set; }

        [MaxLength(10)]
        public string? PhoneVerificationToken { get; set; }

        [MaxLength(255)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetExpiresAt { get; set; }

        public DateTime? LockedUntil { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastFailedLoginAttempt { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;

        [MaxLength(255)]
        public string? TwoFactorSecret { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties (Auth Service only - removed references to Lottery, Payment, etc.)
        public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public virtual ICollection<UserPhone> Phones { get; set; } = new List<UserPhone>();
        public virtual ICollection<UserIdentityDocument> IdentityDocuments { get; set; } = new List<UserIdentityDocument>();
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
    }

    [Table("user_addresses")]
    public class UserAddress
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "home";

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(255)]
        public string? Street { get; set; }

        [MaxLength(20)]
        public string? HouseNumber { get; set; }

        [MaxLength(20)]
        public string? ZipCode { get; set; }

        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    [Table("user_phones")]
    public class UserPhone
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        public bool IsVerified { get; set; } = false;

        [MaxLength(10)]
        public string? VerificationCode { get; set; }

        public DateTime? VerificationExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    [Table("user_identity_documents")]
    public class UserIdentityDocument
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DocumentNumber { get; set; } = string.Empty;

        // Image URLs - deprecated, images not stored (processed in-memory only)
        public string? FrontImageUrl { get; set; }

        public string? BackImageUrl { get; set; }

        public string? SelfieImageUrl { get; set; }

        // Verification metadata
        [Required]
        public Guid ValidationKey { get; set; } = Guid.NewGuid();

        [Column(TypeName = "decimal(5,2)")]
        public decimal? LivenessScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? FaceMatchScore { get; set; }

        [MaxLength(50)]
        public string VerificationProvider { get; set; } = "aws_rekognition";

        [Column(TypeName = "jsonb")]
        public string? VerificationMetadata { get; set; }

        public int VerificationAttempts { get; set; } = 0;

        public DateTime? LastVerificationAttempt { get; set; }

        [MaxLength(20)]
        public string VerificationStatus { get; set; } = "pending";

        public DateTime? VerifiedAt { get; set; }

        public Guid? VerifiedBy { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}

