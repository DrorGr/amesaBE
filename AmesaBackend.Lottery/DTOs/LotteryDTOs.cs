using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Lottery.DTOs
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
        public int? MaxParticipants { get; set; }
        public int UniqueParticipants { get; set; }
        public bool IsParticipantCapReached { get; set; }
        public int? RemainingParticipantSlots { get; set; }
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

        [Range(1, int.MaxValue)]
        public int? MaxParticipants { get; set; }
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

        [Range(1, int.MaxValue)]
        public int? MaxParticipants { get; set; }
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
        [StringLength(50)]
        public string DrawMethod { get; set; } = "random";

        [StringLength(255)]
        public string? DrawSeed { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public ErrorResponse? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Details { get; set; }
    }

    /// <summary>
    /// User lottery statistics DTO
    /// </summary>
    public class UserLotteryStatsDto
    {
        public int TotalEntries { get; set; }
        public int ActiveEntries { get; set; }
        public int TotalWins { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal TotalWinnings { get; set; }
        public decimal WinRate { get; set; }
        public decimal AverageSpendingPerEntry { get; set; }
        public Guid? FavoriteHouseId { get; set; }
        public string? MostActiveMonth { get; set; }
        public DateTime? LastEntryDate { get; set; }
    }

    /// <summary>
    /// Entry filters for querying user entries
    /// </summary>
    public class EntryFilters
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
        public string? Status { get; set; }
        public Guid? HouseId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsWinner { get; set; }
    }

    /// <summary>
    /// Lottery participant statistics DTO
    /// </summary>
    public class LotteryParticipantStatsDto
    {
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public int UniqueParticipants { get; set; }
        public int TotalTickets { get; set; }
        public int? MaxParticipants { get; set; }
        public bool IsCapReached { get; set; }
        public int? RemainingSlots { get; set; }
        public DateTime? LastEntryDate { get; set; }
    }

    /// <summary>
    /// Watchlist item DTO
    /// </summary>
    public class WatchlistItemDto
    {
        public Guid Id { get; set; }
        public Guid HouseId { get; set; }
        public HouseDto House { get; set; } = null!;
        public bool NotificationEnabled { get; set; }
        public DateTime AddedAt { get; set; }
    }

    /// <summary>
    /// Response for can-enter lottery check
    /// </summary>
    public class CanEnterLotteryResponse
    {
        public bool CanEnter { get; set; }
        public string? Reason { get; set; }
        public bool IsExistingParticipant { get; set; }
    }

    /// <summary>
    /// Request to add house to watchlist
    /// </summary>
    public class AddToWatchlistRequest
    {
        public bool NotificationEnabled { get; set; } = true;
    }

    /// <summary>
    /// Request to toggle notification for watchlist item
    /// </summary>
    public class ToggleNotificationRequest
    {
        [Required]
        public bool Enabled { get; set; }
    }
}
