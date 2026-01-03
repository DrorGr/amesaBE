namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing a countdown update for a lottery draw.
/// Used for real-time countdown broadcasting to clients via SignalR, showing time remaining until the draw.
/// </summary>
public class CountdownUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the house for which the countdown is being updated.
    /// </summary>
    public Guid HouseId { get; set; }
    
    /// <summary>
    /// Gets or sets the total time remaining until the lottery draw.
    /// </summary>
    public TimeSpan TimeRemaining { get; set; }
    
    /// <summary>
    /// Gets or sets the number of days remaining until the lottery draw.
    /// </summary>
    public int Days { get; set; }
    
    /// <summary>
    /// Gets or sets the number of hours remaining until the lottery draw (within the current day).
    /// </summary>
    public int Hours { get; set; }
    
    /// <summary>
    /// Gets or sets the number of minutes remaining until the lottery draw (within the current hour).
    /// </summary>
    public int Minutes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of seconds remaining until the lottery draw (within the current minute).
    /// </summary>
    public int Seconds { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the lottery is currently active (accepting ticket purchases).
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the lottery has ended (draw has been conducted).
    /// </summary>
    public bool IsEnded { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the lottery draw is scheduled to occur.
    /// </summary>
    public DateTime LotteryEndDate { get; set; }
}
