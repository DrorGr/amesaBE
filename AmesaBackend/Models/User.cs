using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
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

        [MaxLength(10)]
        public string? PhoneVerificationToken { get; set; }

        [MaxLength(255)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetExpiresAt { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;

        [MaxLength(255)]
        public string? TwoFactorSecret { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public virtual ICollection<UserPhone> Phones { get; set; } = new List<UserPhone>();
        public virtual ICollection<UserIdentityDocument> IdentityDocuments { get; set; } = new List<UserIdentityDocument>();
        public virtual ICollection<LotteryTicket> Tickets { get; set; } = new List<LotteryTicket>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<UserPaymentMethod> PaymentMethods { get; set; } = new List<UserPaymentMethod>();
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
        public virtual ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
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

        public string? FrontImageUrl { get; set; }

        public string? BackImageUrl { get; set; }

        public string? SelfieImageUrl { get; set; }

        [MaxLength(20)]
        public string VerificationStatus { get; set; } = "pending";

        public DateTime? VerifiedAt { get; set; }

        public Guid? VerifiedBy { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual User? VerifiedByUser { get; set; }
    }
}
