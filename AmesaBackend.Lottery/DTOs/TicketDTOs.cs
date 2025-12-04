using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmesaBackend.Lottery.DTOs
{
    /// <summary>
    /// Response for favorite house operations - matches API contract
    /// </summary>
    public class FavoriteHouseResponse
    {
        public Guid HouseId { get; set; }
        public bool Added { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Paged entry history response - Fixed: Uses "Items" to match API contract
    /// </summary>
    public class PagedEntryHistoryResponse
    {
        public List<LotteryTicketDto> Items { get; set; } = new(); // Fixed: Changed from "Entries" to "Items"
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }

    /// <summary>
    /// Quick entry request
    /// </summary>
    public class QuickEntryRequest
    {
        [Required]
        public Guid HouseId { get; set; }

        [Range(1, 10)]
        [JsonPropertyName("quantity")]
        public int TicketCount { get; set; } = 1; // Fixed: Added JsonPropertyName to match frontend "quantity"

        [Required]
        public Guid PaymentMethodId { get; set; }
    }

    /// <summary>
    /// Quick entry response - Fixed: Matches API contract structure
    /// </summary>
    public class QuickEntryResponse
    {
        public int TicketsPurchased { get; set; } // Fixed: Changed from List<LotteryTicketDto> to int (count) to match API contract
        public decimal TotalCost { get; set; } // Fixed: Changed from "TotalAmount" to "TotalCost"
        public List<string> TicketNumbers { get; set; } = new(); // Fixed: Added TicketNumbers array
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Recommended house with score and reason
    /// </summary>
    public class RecommendedHouseDto : HouseDto
    {
        public decimal RecommendationScore { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to create tickets from payment transaction
    /// Used by Payment service after payment success
    /// </summary>
    public class CreateTicketsFromPaymentRequest
    {
        [Required]
        public Guid HouseId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }

        [Required]
        public Guid PaymentId { get; set; }  // Payment transaction ID

        [Required]
        public Guid UserId { get; set; }
        
        /// <summary>
        /// Optional reservation token for inventory confirmation
        /// Used to confirm inventory reservation after successful ticket creation
        /// </summary>
        public string? ReservationToken { get; set; }
    }

    /// <summary>
    /// Response when tickets are created from payment
    /// </summary>
    public class CreateTicketsFromPaymentResponse
    {
        public List<string> TicketNumbers { get; set; } = new();
        public int TicketsPurchased { get; set; }
    }

    /// <summary>
    /// Request to validate ticket purchase before payment
    /// Used by Payment service to validate purchase before processing payment
    /// </summary>
    public class ValidateTicketsRequest
    {
        [Required]
        public Guid HouseId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }

        [Required]
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Response from ticket validation
    /// </summary>
    public class ValidateTicketsResponse
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal TotalCost { get; set; }
        public bool CanEnter { get; set; }  // Participant cap check
    }

    /// <summary>
    /// Request to purchase tickets for a house
    /// POST /api/v1/houses/{id}/tickets/purchase
    /// </summary>
    public class PurchaseTicketsRequest
    {
        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }
        
        [Required]
        public Guid PaymentMethodId { get; set; }
    }

    /// <summary>
    /// Response when tickets are purchased
    /// </summary>
    public class PurchaseTicketsResponse
    {
        public int TicketsPurchased { get; set; }
        public decimal TotalCost { get; set; }
        public List<string> TicketNumbers { get; set; } = new();
        public string TransactionId { get; set; } = string.Empty;
        public Guid PaymentId { get; set; }
    }
}

