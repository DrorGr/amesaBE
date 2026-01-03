namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object containing analytics and statistics for a promotion.
/// Provides comprehensive metrics about promotion usage, performance, and revenue impact.
/// </summary>
public class PromotionAnalyticsDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the promotion.
    /// </summary>
    public Guid PromotionId { get; set; }
    
    /// <summary>
    /// Gets or sets the promotion code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of the promotion.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the total number of times this promotion has been used.
    /// </summary>
    public int TotalUsage { get; set; }
    
    /// <summary>
    /// Gets or sets the number of unique users who have used this promotion.
    /// </summary>
    public int UniqueUsers { get; set; }
    
    /// <summary>
    /// Gets or sets the total discount amount applied across all usages of this promotion.
    /// </summary>
    public decimal TotalDiscountAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the average discount amount per usage of this promotion.
    /// Calculated as TotalDiscountAmount / TotalUsage.
    /// </summary>
    public decimal AverageDiscountAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when this promotion was first used.
    /// Null if the promotion has never been used.
    /// </summary>
    public DateTime? FirstUsedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when this promotion was most recently used.
    /// Null if the promotion has never been used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Gets or sets a dictionary mapping dates (formatted as strings) to usage counts for each day.
    /// Key format: "YYYY-MM-DD", Value: number of times used on that day.
    /// </summary>
    public Dictionary<string, int> UsageByDay { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a dictionary mapping dates (formatted as strings) to revenue impact for each day.
    /// Key format: "YYYY-MM-DD", Value: total discount amount applied on that day.
    /// </summary>
    public Dictionary<string, decimal> RevenueByDay { get; set; } = new();
}
