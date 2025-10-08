using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.DTOs
{
    public class HouseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int? SquareFeet { get; set; }
        public string? PropertyType { get; set; }
        public int? YearBuilt { get; set; }
        public decimal? LotSize { get; set; }
        public string[]? Features { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
        public decimal TicketPrice { get; set; }
        public DateTime? LotteryStartDate { get; set; }
        public DateTime LotteryEndDate { get; set; }
        public DateTime? DrawDate { get; set; }
        public decimal MinimumParticipationPercentage { get; set; }
        public int TicketsSold { get; set; }
        public decimal ParticipationPercentage { get; set; }
        public bool CanExecute { get; set; }
        public List<HouseImageDto> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class HouseImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public int? FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    public class CreateHouseRequest
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(255)]
        public string Location { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Bedrooms { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Bathrooms { get; set; }

        [Range(1, int.MaxValue)]
        public int? SquareFeet { get; set; }

        [StringLength(50)]
        public string? PropertyType { get; set; }

        [Range(1800, 2100)]
        public int? YearBuilt { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? LotSize { get; set; }

        public string[]? Features { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int TotalTickets { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TicketPrice { get; set; }

        public DateTime? LotteryStartDate { get; set; }

        [Required]
        public DateTime LotteryEndDate { get; set; }

        [Range(0.01, 100.00)]
        public decimal MinimumParticipationPercentage { get; set; } = 75.00m;
    }

    public class UpdateHouseRequest
    {
        [StringLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        public string? Address { get; set; }

        [Range(1, int.MaxValue)]
        public int? Bedrooms { get; set; }

        [Range(1, int.MaxValue)]
        public int? Bathrooms { get; set; }

        [Range(1, int.MaxValue)]
        public int? SquareFeet { get; set; }

        [StringLength(50)]
        public string? PropertyType { get; set; }

        [Range(1800, 2100)]
        public int? YearBuilt { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? LotSize { get; set; }

        public string[]? Features { get; set; }

        [Range(1, int.MaxValue)]
        public int? TotalTickets { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? TicketPrice { get; set; }

        public DateTime? LotteryStartDate { get; set; }

        public DateTime? LotteryEndDate { get; set; }

        [Range(0.01, 100.00)]
        public decimal? MinimumParticipationPercentage { get; set; }
    }

    public class LotteryTicketDto
    {
        public Guid Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public bool IsWinner { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PurchaseTicketRequest
    {
        [Required]
        public Guid HouseId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [Required]
        public Guid PaymentMethodId { get; set; }

        [StringLength(50)]
        public string? PromotionCode { get; set; }
    }

    public class LotteryDrawDto
    {
        public Guid Id { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public DateTime DrawDate { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalParticipationPercentage { get; set; }
        public string? WinningTicketNumber { get; set; }
        public Guid? WinnerUserId { get; set; }
        public string? WinnerName { get; set; }
        public string DrawStatus { get; set; } = string.Empty;
        public string DrawMethod { get; set; } = string.Empty;
        public DateTime? ConductedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ConductDrawRequest
    {
        [Required]
        public Guid DrawId { get; set; }

        [StringLength(50)]
        public string DrawMethod { get; set; } = "random";

        [StringLength(255)]
        public string? DrawSeed { get; set; }
    }
}
