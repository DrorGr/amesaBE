namespace AmesaBackend.Lottery.DTOs;

public class ValidatePromotionRequest
{
    public string Code { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? HouseId { get; set; }
    public decimal Amount { get; set; }
}
