namespace AmesaBackend.Lottery.DTOs;

public class ReservationStatusUpdate
{
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime Timestamp { get; set; }
}
