using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services;

/// <summary>
/// Interface for gamification service
/// Handles points, levels, tiers, streaks, and achievements
/// </summary>
public interface IGamificationService
{
    /// <summary>
    /// Award points to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="points">Points to award (can be negative for deductions)</param>
    /// <param name="reason">Reason for points change (e.g., "Ticket Purchase", "Lottery Win")</param>
    /// <param name="referenceId">Optional reference ID (ticket ID, draw ID, etc.)</param>
    Task AwardPointsAsync(Guid userId, int points, string reason, Guid? referenceId = null);

    /// <summary>
    /// Calculate user level based on total points
    /// Formula: floor(sqrt(total_points / 100)) + 1 (max 100)
    /// </summary>
    /// <param name="points">Total points</param>
    /// <returns>User level (1-100)</returns>
    Task<int> CalculateLevelAsync(int points);

    /// <summary>
    /// Calculate user tier based on level or points
    /// Tiers: Bronze (0-500), Silver (501-2,000), Gold (2,001-5,000), Platinum (5,001-10,000), Diamond (10,001+)
    /// </summary>
    /// <param name="points">Total points</param>
    /// <returns>Tier name</returns>
    Task<string> CalculateTierAsync(int points);

    /// <summary>
    /// Update user streak based on last entry date
    /// Increments if last entry was yesterday, resets if longer gap
    /// </summary>
    /// <param name="userId">User ID</param>
    Task UpdateStreakAsync(Guid userId);

    /// <summary>
    /// Check and unlock achievements based on action
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="actionType">Action type (e.g., "EntryPurchase", "Win", "FavoriteAdded")</param>
    /// <param name="actionData">Action-specific data (e.g., ticket count, win count)</param>
    /// <returns>List of newly unlocked achievements</returns>
    Task<List<AchievementDto>> CheckAchievementsAsync(Guid userId, string actionType, object? actionData = null);

    /// <summary>
    /// Get user gamification data
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User gamification DTO</returns>
    Task<UserGamificationDto> GetUserGamificationAsync(Guid userId);

    /// <summary>
    /// Get user achievements
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user achievements</returns>
    Task<List<AchievementDto>> GetUserAchievementsAsync(Guid userId);
}

