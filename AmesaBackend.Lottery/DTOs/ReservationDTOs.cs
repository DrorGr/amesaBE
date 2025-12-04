using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Lottery.DTOs
{
    public class ReservationDto
    {
        public Guid Id { get; set; }
        public Guid HouseId { get; set; }
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public Guid? PaymentMethodId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ReservationToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Guid? PaymentTransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateReservationRequest
    {
        [Required]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; }

        public Guid? PaymentMethodId { get; set; }
    }

    public class InventoryStatus
    {
        public Guid HouseId { get; set; }
        public int TotalTickets { get; set; }
        public int AvailableTickets { get; set; }
        public int ReservedTickets { get; set; }
        public int SoldTickets { get; set; }
        public DateTime LotteryEndDate { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public bool IsSoldOut { get; set; }
        public bool IsEnded { get; set; }
    }

    public class InventoryUpdate
    {
        public Guid HouseId { get; set; }
        public int AvailableTickets { get; set; }
        public int ReservedTickets { get; set; }
        public int SoldTickets { get; set; }
        public bool IsSoldOut { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CountdownUpdate
    {
        public Guid HouseId { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public bool IsEnded { get; set; }
        public DateTime LotteryEndDate { get; set; }
    }

    public class ReservationStatusUpdate
    {
        public Guid ReservationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}





