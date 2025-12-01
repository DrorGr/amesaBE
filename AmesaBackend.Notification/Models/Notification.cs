using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Notification.Models
{
    [Table("notification_templates")]
    public class NotificationTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public string[]? Variables { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(50)]
        public string Channel { get; set; } = "all";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
    }

    [Table("user_notifications")]
    public class UserNotification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? TemplateId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Data { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual NotificationTemplate? Template { get; set; }
    }

    [Table("email_templates")]
    public class EmailTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        public string? BodyHtml { get; set; }

        public string? BodyText { get; set; }

        public string[]? Variables { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "jsonb")]
        public string? ChannelSpecific { get; set; }
    }

    [Table("notification_deliveries", Schema = "amesa_notification")]
    public class NotificationDelivery
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid NotificationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Channel { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending";

        [MaxLength(255)]
        public string? ExternalId { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? OpenedAt { get; set; }

        public DateTime? ClickedAt { get; set; }

        public int RetryCount { get; set; } = 0;

        [Column(TypeName = "decimal(10,6)")]
        public decimal? Cost { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual UserNotification? Notification { get; set; }
    }

    [Table("user_channel_preferences", Schema = "amesa_notification")]
    public class UserChannelPreference
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Channel { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        [Column(TypeName = "jsonb")]
        public string? NotificationTypes { get; set; }

        public TimeSpan? QuietHoursStart { get; set; }

        public TimeSpan? QuietHoursEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("push_subscriptions", Schema = "amesa_notification")]
    public class PushSubscription
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public string P256dhKey { get; set; } = string.Empty;

        [Required]
        public string AuthKey { get; set; } = string.Empty;

        public string? UserAgent { get; set; }

        [Column(TypeName = "jsonb")]
        public string? DeviceInfo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("telegram_user_links", Schema = "amesa_notification")]
    public class TelegramUserLink
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public long TelegramUserId { get; set; }

        [MaxLength(255)]
        public string? TelegramUsername { get; set; }

        [Required]
        public long ChatId { get; set; }

        public bool Verified { get; set; } = false;

        [MaxLength(100)]
        public string? VerificationToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("social_media_links", Schema = "amesa_notification")]
    public class SocialMediaLink
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Platform { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PlatformUserId { get; set; } = string.Empty;

        public string? AccessToken { get; set; }

        public DateTime? TokenExpiresAt { get; set; }

        public bool Verified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("notification_queue", Schema = "amesa_notification")]
    public class NotificationQueue
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid NotificationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Channel { get; set; } = string.Empty;

        public int Priority { get; set; } = 5;

        public DateTime? ScheduledFor { get; set; }

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 3;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending";

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public virtual UserNotification? Notification { get; set; }
    }
}

