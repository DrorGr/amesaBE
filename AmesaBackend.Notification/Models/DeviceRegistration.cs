using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Notification.Models
{
    [Table("device_registrations", Schema = "amesa_notification")]
    public class DeviceRegistration
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Platform { get; set; } = string.Empty; // "iOS", "Android", "Web"

        [MaxLength(255)]
        public string? DeviceId { get; set; }

        [MaxLength(255)]
        public string? DeviceName { get; set; }

        [MaxLength(50)]
        public string? AppVersion { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }
    }
}






