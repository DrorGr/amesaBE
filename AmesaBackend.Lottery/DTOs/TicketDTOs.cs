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
        public int TicketsPurchased { get; set; } // Fixed: Changed from List<LotteryTicketDto> to int (count)
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
}

