using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("promotions", Schema = "amesa_admin")]
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

        // amesa_admin.promotions.value
        [Column("value", TypeName = "decimal(10,2)")]
        public decimal? Value { get; set; }

        [MaxLength(20)]
        [Column("value_type")]
        public string? ValueType { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("usage_limit")]
        public int? UsageLimit { get; set; }

        [Column("usage_count")]
        public int UsageCount { get; set; } = 0;

        [Column("min_purchase_amount", TypeName = "decimal(10,2)")]
        public decimal? MinPurchaseAmount { get; set; }

        [Column("max_discount_amount", TypeName = "decimal(10,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Column("applicable_houses")]
        public Guid[]? ApplicableHouses { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
    }

    [Table("user_promotions", Schema = "amesa_admin")]
    public class UserPromotion
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("promotion_id")]
        public Guid PromotionId { get; set; }

        [Column("used_at")]
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        [Column("transaction_id")]
        public Guid? TransactionId { get; set; }

        [Column("discount_amount", TypeName = "decimal(10,2)")]
        public decimal? DiscountAmount { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Transaction? Transaction { get; set; }
    }
}
