using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models;

[Table("product_links", Schema = "amesa_payment")]
public class ProductLink
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string LinkedEntityType { get; set; } = string.Empty;

    [Required]
    public Guid LinkedEntityId { get; set; }

    [Column(TypeName = "jsonb")]
    public string? LinkMetadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Product Product { get; set; } = null!;
}

