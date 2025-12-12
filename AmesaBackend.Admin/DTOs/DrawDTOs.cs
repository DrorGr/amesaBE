namespace AmesaBackend.Admin.DTOs
{
    public class DrawDto
    {
        public Guid Id { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public DateTime DrawDate { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalParticipationPercentage { get; set; }
        public string? WinningTicketNumber { get; set; }
        public Guid? WinningTicketId { get; set; }
        public Guid? WinnerUserId { get; set; }
        public string DrawStatus { get; set; } = string.Empty;
        public string DrawMethod { get; set; } = string.Empty;
        public Guid? ConductedBy { get; set; }
        public DateTime? ConductedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

