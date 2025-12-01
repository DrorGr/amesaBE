using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("products", Schema = "amesa_payment")]
public class Product
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty; // 'lottery_ticket', 'subscription', 'timed_event', 'custom'

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // 'active', 'inactive', 'archived'

    [Required]
    [Column(TypeName = "decimal(15,2)")]
    public decimal BasePrice { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    public string? PricingModel { get; set; }

    [Column(TypeName = "jsonb")]
    public string? PricingMetadata { get; set; }

    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public int? MaxQuantityPerUser { get; set; }
    public int? TotalQuantityAvailable { get; set; }
    public int QuantitySold { get; set; } = 0;

    [Column(TypeName = "jsonb")]
    public string? ProductMetadata { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ProductLink> Links { get; set; } = new List<ProductLink>();
    public virtual ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
}

