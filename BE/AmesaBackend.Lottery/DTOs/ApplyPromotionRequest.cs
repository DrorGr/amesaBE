namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Request object for applying a promotion code to a purchase.
/// Contains all information needed to validate and apply a promotion to a transaction.
/// </summary>
public class ApplyPromotionRequest
{
    /// <summary>
    /// Gets or sets the promotion code to apply.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the unique identifier of the user applying the promotion.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the optional unique identifier of the house for house-specific promotions.
    /// </summary>
    public Guid? HouseId { get; set; }
    
    /// <summary>
    /// Gets or sets the total purchase amount before discount.
    /// Used to validate minimum purchase requirements.
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the transaction to which the promotion is being applied.
    /// </summary>
    public Guid TransactionId { get; set; }
    
    /// <summary>
    /// Gets or sets the calculated discount amount to apply.
    /// This value is validated against the promotion's rules and limits.
    /// </summary>
    public decimal DiscountAmount { get; set; }
}
