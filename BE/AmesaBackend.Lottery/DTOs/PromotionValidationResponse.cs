namespace AmesaBackend.Lottery.DTOs;

public class PromotionValidationResponse
{
    public bool IsValid { get; set; }
    public PromotionDto? Promotion { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}
