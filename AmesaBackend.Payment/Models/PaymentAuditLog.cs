using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("payment_audit_log", Schema = "amesa_payment")]
public class PaymentAuditLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? Amount { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

