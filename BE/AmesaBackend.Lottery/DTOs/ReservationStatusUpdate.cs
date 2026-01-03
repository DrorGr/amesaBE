namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing a status update for a ticket reservation.
/// Used for real-time notifications to users about changes in their reservation status via SignalR.
/// </summary>
public class ReservationStatusUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the reservation whose status was updated.
    /// </summary>
    public Guid ReservationId { get; set; }
    
    /// <summary>
    /// Gets or sets the current status of the reservation (e.g., "pending", "confirmed", "expired", "failed").
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a human-readable message describing the status update.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets an optional error message if the reservation processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the reservation was processed (confirmed or failed).
    /// Null if the reservation is still pending.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when this status update was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
