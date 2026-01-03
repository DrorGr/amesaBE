namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Request object for validating a promotion code before applying it.
/// Used to check if a promotion is valid and applicable to a specific purchase without actually applying it.
/// </summary>
public class ValidatePromotionRequest
{
    /// <summary>
    /// Gets or sets the promotion code to validate.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the unique identifier of the user attempting to use the promotion.
    /// Used to check user-specific usage limits and eligibility.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the optional unique identifier of the house for house-specific promotions.
    /// </summary>
    public Guid? HouseId { get; set; }
    
    /// <summary>
    /// Gets or sets the total purchase amount before discount.
    /// Used to validate minimum purchase requirements and calculate applicable discount.
    /// </summary>
    public decimal Amount { get; set; }
}
