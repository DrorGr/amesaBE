namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing a single usage of a promotion.
/// Records when and how a promotion was applied to a transaction.
/// </summary>
public class PromotionUsageDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the promotion usage record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the promotion that was used.
    /// </summary>
    public Guid PromotionId { get; set; }
    
    /// <summary>
    /// Gets or sets the promotion code that was used.
    /// </summary>
    public string PromotionCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the unique identifier of the user who used the promotion.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the transaction where the promotion was applied.
    /// </summary>
    public Guid? TransactionId { get; set; }
    
    /// <summary>
    /// Gets or sets the discount amount that was applied from this promotion usage.
    /// </summary>
    public decimal DiscountAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the promotion was used.
    /// </summary>
    public DateTime UsedAt { get; set; }
}
