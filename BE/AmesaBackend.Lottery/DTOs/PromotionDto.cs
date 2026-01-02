namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing a lottery promotion or discount.
/// Contains all information about a promotion including its type, value, usage limits, and validity period.
/// </summary>
public class PromotionDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the promotion.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique promotion code that users can enter to apply the promotion.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of the promotion.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optional description of the promotion.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the type of promotion (e.g., "Percentage", "FixedAmount", "FreeShipping").
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the discount value of the promotion.
    /// Interpretation depends on ValueType (percentage or fixed amount).
    /// </summary>
    public decimal? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the type of value (e.g., "Percentage", "FixedAmount").
    /// Determines how the Value property is interpreted.
    /// </summary>
    public string? ValueType { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum purchase amount required to use this promotion.
    /// </summary>
    public decimal? MinAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum discount amount that can be applied.
    /// Used to cap the discount for percentage-based promotions.
    /// </summary>
    public decimal? MaxDiscount { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of times this promotion can be used.
    /// Null indicates unlimited usage.
    /// </summary>
    public int? UsageLimit { get; set; }
    
    /// <summary>
    /// Gets or sets the current number of times this promotion has been used.
    /// </summary>
    public int UsageCount { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the promotion is currently active.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the start date when the promotion becomes valid.
    /// Null indicates the promotion is valid immediately.
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Gets or sets the end date when the promotion expires.
    /// Null indicates the promotion has no expiration date.
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Gets or sets an array of house IDs where this promotion is applicable.
    /// Null or empty array indicates the promotion is applicable to all houses.
    /// </summary>
    public Guid[]? ApplicableHouses { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the promotion was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the promotion was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
