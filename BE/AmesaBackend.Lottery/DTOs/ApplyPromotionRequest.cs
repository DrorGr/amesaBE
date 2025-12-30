namespace AmesaBackend.Lottery.DTOs;

public class ApplyPromotionRequest
{
    public string Code { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? HouseId { get; set; }
    public decimal Amount { get; set; }
    public Guid TransactionId { get; set; }
    public decimal DiscountAmount { get; set; }
}
