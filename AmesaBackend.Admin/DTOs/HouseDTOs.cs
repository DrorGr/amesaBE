using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Admin.DTOs
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
        public int? MaxParticipants { get; set; }
        public List<HouseImageDto> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class HouseImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
        public string MediaType { get; set; } = string.Empty;
    }

    public class CreateHouseRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        [Required(ErrorMessage = "Location is required")]
        [MaxLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
        public string Location { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int? SquareFeet { get; set; }
        public string? PropertyType { get; set; }
        public int? YearBuilt { get; set; }
        public decimal? LotSize { get; set; }
        public string[]? Features { get; set; }
        public string? Status { get; set; }
        [Required(ErrorMessage = "Total tickets is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Total tickets must be greater than 0")]
        public int TotalTickets { get; set; }
        [Required(ErrorMessage = "Ticket price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ticket price must be greater than 0")]
        public decimal TicketPrice { get; set; }
        public DateTime? LotteryStartDate { get; set; }
        public DateTime LotteryEndDate { get; set; }
        public DateTime? DrawDate { get; set; }
        public decimal? MinimumParticipationPercentage { get; set; }
        public int? MaxParticipants { get; set; }
    }

    public class UpdateHouseRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }
        public string? Location { get; set; }
        public string? Address { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? SquareFeet { get; set; }
        public string? PropertyType { get; set; }
        public int? YearBuilt { get; set; }
        public decimal? LotSize { get; set; }
        public string[]? Features { get; set; }
        public string? Status { get; set; }
        public int? TotalTickets { get; set; }
        public decimal? TicketPrice { get; set; }
        public DateTime? LotteryStartDate { get; set; }
        public DateTime? LotteryEndDate { get; set; }
        public DateTime? DrawDate { get; set; }
        public decimal? MinimumParticipationPercentage { get; set; }
        public int? MaxParticipants { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

