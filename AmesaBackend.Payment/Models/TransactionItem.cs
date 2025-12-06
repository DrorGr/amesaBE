using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("transaction_items", Schema = "amesa_payment")]
public class TransactionItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("product_id")]
    public Guid? ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("item_type")]
    public string ItemType { get; set; } = string.Empty; // 'product', 'fee', 'discount', 'tax'

    [Required]
    [MaxLength(255)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column("unit_price", TypeName = "decimal(15,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("total_price", TypeName = "decimal(15,2)")]
    public decimal TotalPrice { get; set; }

    [MaxLength(50)]
    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }

    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    [Column("item_metadata", TypeName = "jsonb")]
    public string? ItemMetadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Transaction Transaction { get; set; } = null!;
    public virtual Product? Product { get; set; }
}

