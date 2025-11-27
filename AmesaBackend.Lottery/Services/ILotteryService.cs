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
        Task<List<HouseDto>> GetUserFavoriteHousesAsync(Guid userId);
        Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId);
        Task<bool> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId);
        Task<List<HouseDto>> GetRecommendedHousesAsync(Guid userId, int limit = 10);
        
        // Entry management methods
        Task<List<LotteryTicketDto>> GetUserActiveEntriesAsync(Guid userId);
        Task<UserLotteryStatsDto> GetUserLotteryStatsAsync(Guid userId);
        
        // Verification check
        Task CheckVerificationRequirementAsync(Guid userId);
        
        // Participant cap methods
        Task<bool> IsParticipantCapReachedAsync(Guid houseId);
        Task<int> GetParticipantCountAsync(Guid houseId);
        Task<bool> CanUserEnterLotteryAsync(Guid userId, Guid houseId);
        Task<LotteryParticipantStatsDto> GetParticipantStatsAsync(Guid houseId);
    }
}
