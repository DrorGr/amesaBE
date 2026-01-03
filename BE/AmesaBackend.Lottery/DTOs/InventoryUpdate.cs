namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing an inventory update for a lottery house.
/// Used for real-time inventory synchronization and broadcasting updates to clients via SignalR.
/// </summary>
public class InventoryUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the house whose inventory was updated.
    /// </summary>
    public Guid HouseId { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tickets currently available for purchase.
    /// Calculated as TotalTickets - TicketsSold - ReservedTickets.
    /// </summary>
    public int AvailableTickets { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of tickets available for this house.
    /// </summary>
    public int TotalTickets { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tickets that have been sold (confirmed purchases).
    /// </summary>
    public int TicketsSold { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tickets that are currently reserved (pending confirmation).
    /// </summary>
    public int ReservedTickets { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the house is sold out (no tickets available).
    /// True when AvailableTickets is 0.
    /// </summary>
    public bool IsSoldOut { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the inventory was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
