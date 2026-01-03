namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Data transfer object representing a user achievement in the gamification system.
/// Contains information about achievements that users can unlock through various actions.
/// </summary>
public class AchievementDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the achievement.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of achievement (e.g., "EntryPurchase", "Win", "FavoriteAdded", "Streak").
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of the achievement.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of what the user needs to do to unlock this achievement.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optional icon URL or identifier for displaying the achievement badge.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the user unlocked this achievement.
    /// Null if the achievement has not been unlocked yet.
    /// </summary>
    public DateTime? UnlockedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the optional category of the achievement (e.g., "Purchase", "Social", "Milestone").
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Data transfer object containing comprehensive gamification data for a user.
/// Includes points, level, tier, streaks, and recent achievements.
/// </summary>
public class UserGamificationDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the total points accumulated by the user across all activities.
    /// </summary>
    public int TotalPoints { get; set; }
    
    /// <summary>
    /// Gets or sets the current level of the user (1-100).
    /// Calculated based on total points using the formula: floor(sqrt(total_points / 100)) + 1.
    /// </summary>
    public int CurrentLevel { get; set; }
    
    /// <summary>
    /// Gets or sets the current tier of the user (Bronze, Silver, Gold, Platinum, Diamond).
    /// Determined based on total points.
    /// </summary>
    public string CurrentTier { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the current consecutive day streak for the user.
    /// Increments when the user makes an entry on consecutive days.
    /// </summary>
    public int CurrentStreak { get; set; }
    
    /// <summary>
    /// Gets or sets the longest consecutive day streak the user has achieved.
    /// </summary>
    public int LongestStreak { get; set; }
    
    /// <summary>
    /// Gets or sets the date of the user's last lottery entry.
    /// Used to calculate and maintain streak information.
    /// </summary>
    public DateTime? LastEntryDate { get; set; }
    
    /// <summary>
    /// Gets or sets the list of recently unlocked achievements for the user.
    /// </summary>
    public List<AchievementDto> RecentAchievements { get; set; } = new();
}
