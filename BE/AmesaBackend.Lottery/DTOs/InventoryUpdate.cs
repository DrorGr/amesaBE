namespace AmesaBackend.Lottery.DTOs;

public class InventoryUpdate
{
    public Guid HouseId { get; set; }
    public int AvailableTickets { get; set; }
    public int TotalTickets { get; set; }
    public int TicketsSold { get; set; }
    public int ReservedTickets { get; set; }
    public bool IsSoldOut { get; set; }
    public DateTime LastUpdated { get; set; }
}
