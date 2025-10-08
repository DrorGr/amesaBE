using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("promotions")]
    public class Promotion
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Value { get; set; }

        [MaxLength(20)]
        public string? ValueType { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? UsageLimit { get; set; }

        public int UsageCount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MinPurchaseAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        public Guid[]? ApplicableHouses { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
    }

    [Table("user_promotions")]
    public class UserPromotion
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        public Guid? TransactionId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountAmount { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Transaction? Transaction { get; set; }
    }
}
