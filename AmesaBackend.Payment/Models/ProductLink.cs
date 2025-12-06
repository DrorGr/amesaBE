using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("product_links", Schema = "amesa_payment")]
public class ProductLink
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("linked_entity_type")]
    public string LinkedEntityType { get; set; } = string.Empty;

    [Required]
    [Column("linked_entity_id")]
    public Guid LinkedEntityId { get; set; }

    [Column("link_metadata", TypeName = "jsonb")]
    public string? LinkMetadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Product Product { get; set; } = null!;
}

