using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("products", Schema = "amesa_payment")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("product_type")]
    public string ProductType { get; set; } = string.Empty; // 'lottery_ticket', 'subscription', 'timed_event', 'custom'

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "active"; // 'active', 'inactive', 'archived'

    [Required]
    [Column("base_price", TypeName = "decimal(15,2)")]
    public decimal BasePrice { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    [Column("pricing_model")]
    public string? PricingModel { get; set; }

    [Column("pricing_metadata", TypeName = "jsonb")]
    public string? PricingMetadata { get; set; }

    [Column("available_from")]
    public DateTime? AvailableFrom { get; set; }
    [Column("available_until")]
    public DateTime? AvailableUntil { get; set; }
    [Column("max_quantity_per_user")]
    public int? MaxQuantityPerUser { get; set; }
    [Column("total_quantity_available")]
    public int? TotalQuantityAvailable { get; set; }
    [Column("quantity_sold")]
    public int QuantitySold { get; set; } = 0;

    [Column("product_metadata", TypeName = "jsonb")]
    public string? ProductMetadata { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ProductLink> Links { get; set; } = new List<ProductLink>();
    public virtual ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
}

