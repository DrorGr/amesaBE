using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("transaction_items", Schema = "amesa_payment")]
public class TransactionItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TransactionId { get; set; }

    public Guid? ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = string.Empty; // 'product', 'fee', 'discount', 'tax'

    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(15,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(15,2)")]
    public decimal TotalPrice { get; set; }

    [MaxLength(50)]
    public string? LinkedEntityType { get; set; }

    public Guid? LinkedEntityId { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ItemMetadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Transaction Transaction { get; set; } = null!;
    public virtual Product? Product { get; set; }
}

