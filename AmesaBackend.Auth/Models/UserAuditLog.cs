using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Auth.Models
{
    [Table("user_audit_logs")]
    public class UserAuditLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [Column(TypeName = "text")]
        public string? UserAgent { get; set; }

        [MaxLength(255)]
        public string? DeviceId { get; set; }

        public bool Success { get; set; }

        [Column(TypeName = "text")]
        public string? FailureReason { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
    }
}

