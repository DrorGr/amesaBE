using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("user_sessions")]
    public class UserSession
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string SessionToken { get; set; } = string.Empty;

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        [MaxLength(50)]
        public string? DeviceType { get; set; }

        [MaxLength(100)]
        public string? Browser { get; set; }

        [MaxLength(100)]
        public string? Os { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
    }

    [Table("user_activity_logs")]
    public class UserActivityLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        public Guid? SessionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ResourceType { get; set; }

        public Guid? ResourceId { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Details { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual UserSession? Session { get; set; }
    }

    [Table("system_settings")]
    public class SystemSetting
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        [MaxLength(50)]
        public string Type { get; set; } = "string";

        public string? Description { get; set; }

        public bool IsPublic { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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
    }
}
