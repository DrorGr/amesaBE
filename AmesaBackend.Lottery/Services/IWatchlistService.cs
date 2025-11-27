using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services
{
    public interface IWatchlistService
    {
        /// <summary>
        /// Add house to user's watchlist
        /// </summary>
        Task<bool> AddToWatchlistAsync(Guid userId, Guid houseId, bool notificationEnabled = true);

        /// <summary>
        /// Remove house from user's watchlist
        /// </summary>
        Task<bool> RemoveFromWatchlistAsync(Guid userId, Guid houseId);

        /// <summary>
        /// Get user's watchlist
        /// </summary>
        Task<List<HouseDto>> GetUserWatchlistAsync(Guid userId);

        /// <summary>
        /// Get user's watchlist items with full details
        /// </summary>
        Task<List<WatchlistItemDto>> GetUserWatchlistItemsAsync(Guid userId);

        /// <summary>
        /// Check if house is in user's watchlist
        /// </summary>
        Task<bool> IsInWatchlistAsync(Guid userId, Guid houseId);

        /// <summary>
        /// Toggle notification for watchlist item
        /// </summary>
        Task<bool> ToggleNotificationAsync(Guid userId, Guid houseId, bool enabled);

        /// <summary>
        /// Get watchlist count for user
        /// </summary>
        Task<int> GetWatchlistCountAsync(Guid userId);
    }
}

