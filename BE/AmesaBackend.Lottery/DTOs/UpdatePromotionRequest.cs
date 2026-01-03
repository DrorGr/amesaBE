namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Request object for updating an existing promotion.
/// All properties are optional - only provided properties will be updated.
/// </summary>
public class UpdatePromotionRequest
{
    /// <summary>
    /// Gets or sets the new display name of the promotion.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the new description of the promotion.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the new type of promotion (e.g., "Percentage", "FixedAmount", "FreeShipping").
    /// </summary>
    public string? Type { get; set; }
    
    /// <summary>
    /// Gets or sets the new discount value of the promotion.
    /// Interpretation depends on ValueType (percentage or fixed amount).
    /// </summary>
    public decimal? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the new type of value (e.g., "Percentage", "FixedAmount").
    /// Determines how the Value property is interpreted.
    /// </summary>
    public string? ValueType { get; set; }
    
    /// <summary>
    /// Gets or sets the new minimum purchase amount required to use this promotion.
    /// </summary>
    public decimal? MinAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the new maximum discount amount that can be applied.
    /// Used to cap the discount for percentage-based promotions.
    /// </summary>
    public decimal? MaxDiscount { get; set; }
    
    /// <summary>
    /// Gets or sets the new maximum number of times this promotion can be used.
    /// Null indicates unlimited usage.
    /// </summary>
    public int? UsageLimit { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the promotion is active.
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the new start date when the promotion becomes valid.
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Gets or sets the new end date when the promotion expires.
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Gets or sets a new array of house IDs where this promotion is applicable.
    /// Empty array indicates the promotion is applicable to all houses.
    /// </summary>
    public Guid[]? ApplicableHouses { get; set; }
}
