using AmesaBackend.Auth.DTOs;
using System.Text.Json;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for managing user preferences including lottery-specific preferences
    /// </summary>
    public interface IUserPreferencesService
    {
        /// <summary>
        /// Get user preferences
        /// </summary>
        Task<UserPreferencesDto?> GetUserPreferencesAsync(Guid userId);

        /// <summary>
        /// Update user preferences
        /// </summary>
        Task<UserPreferencesDto> UpdateUserPreferencesAsync(Guid userId, JsonElement preferences, string? version = null);

        /// <summary>
        /// Get lottery preferences for a user
        /// </summary>
        Task<LotteryPreferencesDto?> GetLotteryPreferencesAsync(Guid userId);

        /// <summary>
        /// Update lottery preferences for a user
        /// </summary>
        Task<LotteryPreferencesDto> UpdateLotteryPreferencesAsync(Guid userId, UpdateLotteryPreferencesRequest request);

        /// <summary>
        /// Get favorite house IDs for a user
        /// </summary>
        Task<List<Guid>> GetFavoriteHouseIdsAsync(Guid userId);

        /// <summary>
        /// Add a house to user's favorites
        /// </summary>
        Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId);

        /// <summary>
        /// Remove a house from user's favorites
        /// </summary>
        Task<bool> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId);

        /// <summary>
        /// Check if a house is in user's favorites
        /// </summary>
        Task<bool> IsHouseFavoriteAsync(Guid userId, Guid houseId);

        /// <summary>
        /// Validate lottery preferences structure
        /// </summary>
        PreferenceValidationDto ValidateLotteryPreferences(JsonElement preferences);
    }
}
