namespace AmesaBackend.Lottery.DTOs;

public class PromotionSearchParams
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public bool? IsActive { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
