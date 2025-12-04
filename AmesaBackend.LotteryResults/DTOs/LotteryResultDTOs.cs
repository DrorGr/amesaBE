using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.LotteryResults.DTOs
{
    public class LotteryResultDto
    {
        public Guid Id { get; set; }
        public Guid LotteryId { get; set; }
        public Guid DrawId { get; set; }
        public string WinnerTicketNumber { get; set; } = string.Empty;
        public Guid WinnerUserId { get; set; }
        public int PrizePosition { get; set; }
        public string PrizeType { get; set; } = string.Empty;
        public decimal PrizeValue { get; set; }
        public string? PrizeDescription { get; set; }
        public string QRCodeData { get; set; } = string.Empty;
        public string? QRCodeImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsClaimed { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public DateTime ResultDate { get; set; }
        public string? HouseTitle { get; set; }
        public string? HouseAddress { get; set; }
        public string? WinnerName { get; set; }
        public string? WinnerEmail { get; set; }
    }

    public class LotteryResultsPageDto
    {
        public List<LotteryResultDto> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class LotteryResultsFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public int? PrizePosition { get; set; }
        public string? PrizeType { get; set; }
        public bool? IsClaimed { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "ResultDate";
        public string? SortDirection { get; set; } = "desc";
    }

    public class QRCodeValidationDto
    {
        public bool IsValid { get; set; }
        public bool IsWinner { get; set; }
        public int? PrizePosition { get; set; }
        public string? PrizeType { get; set; }
        public decimal? PrizeValue { get; set; }
        public string? PrizeDescription { get; set; }
        public bool IsClaimed { get; set; }
        public string? Message { get; set; }
        public LotteryResultDto? Result { get; set; }
    }

    public class PrizeDeliveryDto
    {
        public Guid Id { get; set; }
        public Guid LotteryResultId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string DeliveryStatus { get; set; } = string.Empty;
        public DateTime? EstimatedDeliveryDate { get; set; }
        public decimal ShippingCost { get; set; }
        public string? DeliveryNotes { get; set; }
    }

    public class ValidateQRCodeRequest
    {
        [Required]
        public string QRCodeData { get; set; } = string.Empty;
    }

    public class CreatePrizeDeliveryRequest
    {
        [Required]
        public Guid LotteryResultId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string DeliveryMethod { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? DeliveryNotes { get; set; }
    }

    public class ClaimPrizeRequest
    {
        [Required]
        public Guid ResultId { get; set; }

        [MaxLength(1000)]
        public string? ClaimNotes { get; set; }
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
}

