namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Response object containing the result of a promotion validation.
/// Indicates whether a promotion is valid and applicable, along with calculated discount and any error information.
/// </summary>
public class PromotionValidationResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the promotion is valid and can be applied.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Gets or sets the promotion DTO if the promotion was found and is valid.
    /// Null if the promotion code is invalid or the promotion cannot be applied.
    /// </summary>
    public PromotionDto? Promotion { get; set; }
    
    /// <summary>
    /// Gets or sets the calculated discount amount that would be applied.
    /// Only set when IsValid is true.
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Gets or sets a human-readable message explaining the validation result.
    /// Contains success message if valid, or error description if invalid.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Gets or sets an error code if the promotion is invalid.
    /// Common codes: "NOT_FOUND", "EXPIRED", "USAGE_LIMIT_REACHED", "MIN_AMOUNT_NOT_MET", "NOT_APPLICABLE".
    /// </summary>
    public string? ErrorCode { get; set; }
}
