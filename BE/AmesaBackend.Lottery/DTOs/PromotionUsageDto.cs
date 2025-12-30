namespace AmesaBackend.Lottery.DTOs;

public class PromotionUsageDto
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? TransactionId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
}
