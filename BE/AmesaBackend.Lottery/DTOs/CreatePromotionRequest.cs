namespace AmesaBackend.Lottery.DTOs;

public class CreatePromotionRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string? ValueType { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid[]? ApplicableHouses { get; set; }
    public bool IsActive { get; set; } = true;
}
