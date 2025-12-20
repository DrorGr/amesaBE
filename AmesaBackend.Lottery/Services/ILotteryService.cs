using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services
{
    public interface ILotteryService
    {
        Task<List<LotteryTicketDto>> GetUserTicketsAsync(Guid userId);
        Task<LotteryTicketDto> GetTicketAsync(Guid ticketId);
        Task<List<LotteryDrawDto>> GetDrawsAsync();
        Task<LotteryDrawDto> GetDrawAsync(Guid drawId);
        Task ConductDrawAsync(Guid drawId, ConductDrawRequest request);
        
        // Favorites methods
        Task<List<HouseDto>> GetUserFavoriteHousesAsync(Guid userId, int page = 1, int limit = 20, string? sortBy = null, string? sortOrder = null, CancellationToken cancellationToken = default);
        Task<int> GetUserFavoriteHousesCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<DTOs.FavoriteOperationResult> AddHouseToFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default);
        Task<DTOs.FavoriteOperationResult> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default);
        Task<BulkFavoritesResponse> BulkAddFavoritesAsync(Guid userId, List<Guid> houseIds, CancellationToken cancellationToken = default);
        Task<BulkFavoritesResponse> BulkRemoveFavoritesAsync(Guid userId, List<Guid> houseIds, CancellationToken cancellationToken = default);
        Task<List<HouseDto>> GetRecommendedHousesAsync(Guid userId, int limit = 10);
        Task<List<Guid>> GetHouseFavoriteUserIdsAsync(Guid houseId);
        Task<FavoritesAnalyticsDto> GetFavoritesAnalyticsAsync(CancellationToken cancellationToken = default);
        
        // Entry management methods
        Task<List<LotteryTicketDto>> GetUserActiveEntriesAsync(Guid userId);
        Task<UserLotteryStatsDto> GetUserLotteryStatsAsync(Guid userId);
        
        // Verification check
        Task CheckVerificationRequirementAsync(Guid userId);
        
        // Participant cap methods
        Task<bool> IsParticipantCapReachedAsync(Guid houseId);
        Task<int> GetParticipantCountAsync(Guid houseId);
        Task<bool> CanUserEnterLotteryAsync(Guid userId, Guid houseId, bool useTransaction = true);
        Task<LotteryParticipantStatsDto> GetParticipantStatsAsync(Guid houseId);
        Task<List<Guid>> GetHouseParticipantUserIdsAsync(Guid houseId);
        
        // Payment integration methods (called by Payment service)
        Task<CreateTicketsFromPaymentResponse> CreateTicketsFromPaymentAsync(CreateTicketsFromPaymentRequest request);
        Task<ValidateTicketsResponse> ValidateTicketsAsync(ValidateTicketsRequest request);
        
        // Payment processing for Quick Entry
        Task<PaymentProcessResult> ProcessLotteryPaymentAsync(Guid userId, Guid houseId, int ticketCount, Guid paymentMethodId, decimal totalCost);
        
        // Product creation for houses
        Task<Guid?> CreateProductForHouseAsync(Guid houseId, string houseTitle, decimal ticketPrice, Guid? createdBy);
        Task<Guid?> GetProductIdForHouseAsync(Guid houseId);
        Task<Dictionary<Guid, Guid?>> GetProductIdsForHousesAsync(List<Guid> houseIds);
        
        // Draw participants
        Task<List<ParticipantDto>> GetDrawParticipantsAsync(Guid drawId);
    }

    /// <summary>
    /// Result of payment processing
    /// </summary>
    public class PaymentProcessResult
    {
        public bool Success { get; set; }
        public Guid TransactionId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
