using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Models
{
    [Table("user_payment_methods")]
    public class UserPaymentMethod
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public PaymentMethod Type { get; set; }

        [MaxLength(50)]
        public string? Provider { get; set; }

        [MaxLength(255)]
        public string? ProviderPaymentMethodId { get; set; }

        [MaxLength(4)]
        public string? CardLastFour { get; set; }

        [MaxLength(50)]
        public string? CardBrand { get; set; }

        public int? CardExpMonth { get; set; }

        public int? CardExpYear { get; set; }

        public bool IsDefault { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    [Table("transactions")]
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string? Description { get; set; }

        [MaxLength(255)]
        public string? ReferenceId { get; set; }

        public Guid? PaymentMethodId { get; set; }

        [MaxLength(255)]
        public string? ProviderTransactionId { get; set; }

        [Column(TypeName = "jsonb")]
        public string? ProviderResponse { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual UserPaymentMethod? PaymentMethod { get; set; }
    }
}
