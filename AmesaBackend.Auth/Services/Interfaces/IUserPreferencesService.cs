using AmesaBackend.Auth.DTOs;
using System.Text.Json;

namespace AmesaBackend.Auth.Services.Interfaces
{
    /// <summary>
    /// Service for managing user preferences including lottery-specific preferences
    /// </summary>
    public interface IUserPreferencesService
    {
        /// <summary>
        /// Get user preferences
        /// </summary>
        Task<UserPreferencesDto?> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update user preferences
        /// </summary>
        Task<UserPreferencesDto> UpdateUserPreferencesAsync(Guid userId, JsonElement preferences, string? version = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get lottery preferences for a user
        /// </summary>
        Task<LotteryPreferencesDto?> GetLotteryPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update lottery preferences for a user
        /// </summary>
        Task<LotteryPreferencesDto> UpdateLotteryPreferencesAsync(Guid userId, UpdateLotteryPreferencesRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get favorite house IDs for a user
        /// </summary>
        Task<List<Guid>> GetFavoriteHouseIdsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a house to user's favorites
        /// </summary>
        Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a house from user's favorites
        /// </summary>
        Task<bool> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a house is in user's favorites
        /// </summary>
        Task<bool> IsHouseFavoriteAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate lottery preferences structure
        /// </summary>
        PreferenceValidationDto ValidateLotteryPreferences(JsonElement preferences);
    }
}
