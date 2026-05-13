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

    public class HouseTicketStatsDto
    {
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public string HouseStatus { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
        public int BoughtTickets { get; set; }
        public int ReservedTickets { get; set; }
        public int AvailableTickets { get; set; }
        public decimal SoldPercentage { get; set; }
        public int UniqueBuyers { get; set; }
        public decimal AverageTicketsPerBuyer { get; set; }
        public DateTime? LotteryStartDate { get; set; }
        public DateTime LotteryEndDate { get; set; }
        public DateTime? DrawDate { get; set; }
        public DateTime? FirstTicketPurchasedAt { get; set; }
        public DateTime? LastTicketPurchasedAt { get; set; }
        public decimal TicketsPerHour { get; set; }
        public double? HoursUntilLotteryEnd { get; set; }
        public double? HoursToSellOut { get; set; }
        public DateTime? EstimatedSoldOutAt { get; set; }
        public bool IsSoldOut { get; set; }
    }
}

