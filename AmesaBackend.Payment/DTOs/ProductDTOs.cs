using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Payment.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PricingModel { get; set; }
    public Dictionary<string, object>? PricingMetadata { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public int? MaxQuantityPerUser { get; set; }
    public int? TotalQuantityAvailable { get; set; }
    public int QuantitySold { get; set; }
    public Dictionary<string, object>? ProductMetadata { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProductRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    public decimal BasePrice { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    public string? PricingModel { get; set; }

    public Dictionary<string, object>? PricingMetadata { get; set; }

    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxQuantityPerUser { get; set; }

    [Range(0, int.MaxValue)]
    public int? TotalQuantityAvailable { get; set; }

    public Dictionary<string, object>? ProductMetadata { get; set; }
}

public class UpdateProductRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    [Range(0.01, 10000)]
    public decimal? BasePrice { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    [MaxLength(50)]
    public string? PricingModel { get; set; }

    public Dictionary<string, object>? PricingMetadata { get; set; }

    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxQuantityPerUser { get; set; }

    [Range(0, int.MaxValue)]
    public int? TotalQuantityAvailable { get; set; }

    public bool? IsActive { get; set; }

    public Dictionary<string, object>? ProductMetadata { get; set; }
}

public class ProductValidationRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}

public class ProductValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public decimal CalculatedPrice { get; set; }
}

public class ProductValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public decimal CalculatedPrice { get; set; }
}

public class LinkProductRequest
{
    [Required]
    [MaxLength(50)]
    public string LinkedEntityType { get; set; } = string.Empty;

    [Required]
    public Guid LinkedEntityId { get; set; }

    public Dictionary<string, object>? LinkMetadata { get; set; }
}

