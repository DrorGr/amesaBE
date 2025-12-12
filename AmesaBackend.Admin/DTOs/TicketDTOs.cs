namespace AmesaBackend.Admin.DTOs
{
    public class TicketDto
    {
        public Guid Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal PurchasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public bool IsWinner { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

