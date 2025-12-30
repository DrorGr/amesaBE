namespace AmesaBackend.Lottery.DTOs;

public class PromotionAnalyticsDto
{
    public Guid PromotionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalUsage { get; set; }
    public int UniqueUsers { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal AverageDiscountAmount { get; set; }
    public DateTime? FirstUsedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public Dictionary<string, int> UsageByDay { get; set; } = new();
    public Dictionary<string, decimal> RevenueByDay { get; set; } = new();
}
