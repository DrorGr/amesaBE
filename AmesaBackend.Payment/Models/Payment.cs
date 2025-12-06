using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Payment.Models
{
    [Table("user_payment_methods", Schema = "amesa_payment")]
    public class UserPaymentMethod
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("provider")]
        public string? Provider { get; set; }

        [MaxLength(255)]
        [Column("provider_payment_method_id")]
        public string? ProviderPaymentMethodId { get; set; }

        [MaxLength(4)]
        [Column("card_last_four")]
        public string? CardLastFour { get; set; }

        [MaxLength(50)]
        [Column("card_brand")]
        public string? CardBrand { get; set; }

        [Column("card_exp_month")]
        public int? CardExpMonth { get; set; }

        [Column("card_exp_year")]
        public int? CardExpYear { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        [Column("wallet_type")]
        public string? WalletType { get; set; }

        [MaxLength(255)]
        [Column("wallet_account_id")]
        public string? WalletAccountId { get; set; }

        [Column("payment_method_metadata", TypeName = "jsonb")]
        public string? PaymentMethodMetadata { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    [Table("transactions", Schema = "amesa_payment")]
    public class Transaction
    {
        [Key]
        [Column("Id")]
        public Guid Id { get; set; }

        [Required]
        [Column("UserId")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Type")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Column("Amount", TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        [Column("Currency")]
        public string Currency { get; set; } = "USD";

        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Pending";

        [Column("Description")]
        public string? Description { get; set; }

        [MaxLength(255)]
        [Column("ReferenceId")]
        public string? ReferenceId { get; set; }

        [Column("PaymentMethodId")]
        public Guid? PaymentMethodId { get; set; }

        [MaxLength(255)]
        [Column("ProviderTransactionId")]
        public string? ProviderTransactionId { get; set; }

        [Column("ProductId")]
        public Guid? ProductId { get; set; }

        [MaxLength(255)]
        [Column("IdempotencyKey")]
        public string? IdempotencyKey { get; set; }

        [MaxLength(255)]
        [Column("ClientSecret")]
        public string? ClientSecret { get; set; }

        [MaxLength(45)]
        [Column("IpAddress")]
        public string? IpAddress { get; set; }

        [Column("UserAgent")]
        public string? UserAgent { get; set; }

        [Column("ProviderResponse", TypeName = "jsonb")]
        public string? ProviderResponse { get; set; }

        [Column("Metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual UserPaymentMethod? PaymentMethod { get; set; }
        public virtual Product? Product { get; set; }
        public virtual ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
    }
}
