using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Lottery.Models
{
    [Table("promotion_usage_audit", Schema = "amesa_lottery")]
    public class PromotionUsageAudit
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PromotionCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Resolved", "Reversed"

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }

        public Guid? ResolvedByUserId { get; set; }
    }
}






