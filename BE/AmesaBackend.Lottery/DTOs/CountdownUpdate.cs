namespace AmesaBackend.Lottery.DTOs;

public class CountdownUpdate
{
    public Guid HouseId { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public int Days { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public int Seconds { get; set; }
    public bool IsActive { get; set; }
    public bool IsEnded { get; set; }
    public DateTime LotteryEndDate { get; set; }
}
