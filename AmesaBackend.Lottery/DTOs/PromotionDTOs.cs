using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Lottery.DTOs
{
    /// <summary>
    /// Promotion DTO matching frontend interface
    /// Note: Frontend uses 'name', backend uses 'Title' - map Title → name in DTO
    /// </summary>
    public class PromotionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; // Maps from Title
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public decimal? MinAmount { get; set; } // Maps from MinPurchaseAmount
        public decimal? MaxDiscount { get; set; } // Maps from MaxDiscountAmount
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid[]? ApplicableHouses { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PromotionUsageDto
    {
        public Guid Id { get; set; }
        public Guid PromotionId { get; set; }
        public Guid UserId { get; set; }
        public Guid? TransactionId { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; }
    }

    public class CreatePromotionRequest
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; // Maps to Title

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        public decimal? Value { get; set; }

        [MaxLength(20)]
        public string? ValueType { get; set; }

        public decimal? MinAmount { get; set; } // Maps to MinPurchaseAmount

        public decimal? MaxDiscount { get; set; } // Maps to MaxDiscountAmount

        public int? UsageLimit { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public Guid[]? ApplicableHouses { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdatePromotionRequest
    {
        [StringLength(255)]
        public string? Name { get; set; } // Maps to Title

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        public decimal? Value { get; set; }

        [MaxLength(20)]
        public string? ValueType { get; set; }

        public decimal? MinAmount { get; set; } // Maps to MinPurchaseAmount

        public decimal? MaxDiscount { get; set; } // Maps to MaxDiscountAmount

        public int? UsageLimit { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public Guid[]? ApplicableHouses { get; set; }
    }

    public class ValidatePromotionRequest
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        public Guid? HouseId { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Amount { get; set; }
    }

    public class PromotionValidationResponse
    {
        public bool IsValid { get; set; }
        public PromotionDto? Promotion { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
    }

    public class ApplyPromotionRequest
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        public Guid? HouseId { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Amount { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal DiscountAmount { get; set; }
    }

    public class PromotionSearchParams
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
        public bool? IsActive { get; set; }
        public string? Type { get; set; }
        public string? Search { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class PromotionAnalyticsDto
    {
        public Guid PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalUsage { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal AverageDiscount { get; set; }
        public DateTime? FirstUsed { get; set; }
        public DateTime? LastUsed { get; set; }
    }

    public class PromotionUsageAuditDto
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public string PromotionCode { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
        public Guid? ResolvedByUserId { get; set; }
    }

    public class ResolvePromotionAuditRequest
    {
        [Required]
        [StringLength(1000)]
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}

