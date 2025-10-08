using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.DTOs
{
    public class PaymentMethodDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? CardLastFour { get; set; }
        public string? CardBrand { get; set; }
        public int? CardExpMonth { get; set; }
        public int? CardExpYear { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddPaymentMethodRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Provider { get; set; }

        [StringLength(19)]
        public string? CardNumber { get; set; }

        [Range(1, 12)]
        public int? ExpMonth { get; set; }

        [Range(2024, 2030)]
        public int? ExpYear { get; set; }

        [StringLength(4)]
        public string? Cvv { get; set; }

        [StringLength(100)]
        public string? CardholderName { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdatePaymentMethodRequest
    {
        [StringLength(50)]
        public string? Provider { get; set; }

        [Range(1, 12)]
        public int? ExpMonth { get; set; }

        [Range(2024, 2030)]
        public int? ExpYear { get; set; }

        [StringLength(100)]
        public string? CardholderName { get; set; }

        public bool? IsDefault { get; set; }
    }

    public class TransactionDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ReferenceId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProcessPaymentRequest
    {
        [Required]
        public Guid PaymentMethodId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? ReferenceId { get; set; }
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
    }

    public class RefundRequest
    {
        [Required]
        public Guid TransactionId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        [StringLength(255)]
        public string? Reason { get; set; }
    }

    public class WithdrawalRequest
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        [Required]
        public Guid PaymentMethodId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }
    }
}
