namespace AmesaBackend.Lottery.DTOs;

/// <summary>
/// Achievement DTO
/// </summary>
public class AchievementDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// User gamification data DTO
/// </summary>
public class UserGamificationDto
{
    public string UserId { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int CurrentLevel { get; set; }
    public string CurrentTier { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastEntryDate { get; set; }
    public List<AchievementDto> RecentAchievements { get; set; } = new();
}
