using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Lottery.Models
{
    /// <summary>
    /// Entity representing a user's gamification data including points, level, tier, and streaks.
    /// Maps to the user_gamification table in the amesa_lottery schema.
    /// </summary>
    [Table("user_gamification", Schema = "amesa_lottery")]
    public class UserGamification
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user (primary key).
        /// </summary>
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the total points accumulated by the user across all activities.
        /// </summary>
        [Required]
        public int TotalPoints { get; set; } = 0;

        /// <summary>
        /// Gets or sets the current level of the user (1-100).
        /// Calculated based on total points using the formula: floor(sqrt(total_points / 100)) + 1.
        /// </summary>
        [Required]
        public int CurrentLevel { get; set; } = 1;

        /// <summary>
        /// Gets or sets the current tier of the user (Bronze, Silver, Gold, Platinum, Diamond).
        /// Determined based on total points.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string CurrentTier { get; set; } = "Bronze";

        /// <summary>
        /// Gets or sets the current consecutive day streak for the user.
        /// Increments when the user makes an entry on consecutive days.
        /// </summary>
        [Required]
        public int CurrentStreak { get; set; } = 0;

        /// <summary>
        /// Gets or sets the longest consecutive day streak the user has achieved.
        /// </summary>
        [Required]
        public int LongestStreak { get; set; } = 0;

        /// <summary>
        /// Gets or sets the date of the user's last lottery entry.
        /// Used to calculate and maintain streak information.
        /// </summary>
        public DateOnly? LastEntryDate { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the gamification record was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the gamification record was last updated.
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Entity representing a user's unlocked achievement.
    /// Maps to the user_achievements table in the amesa_lottery schema.
    /// </summary>
    [Table("user_achievements", Schema = "amesa_lottery")]
    public class UserAchievement
    {
        /// <summary>
        /// Gets or sets the unique identifier of the achievement record (primary key).
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user who unlocked this achievement.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of achievement (e.g., "EntryPurchase", "Win", "FavoriteAdded", "Streak").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AchievementType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the achievement.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string AchievementName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional icon identifier or URL for displaying the achievement badge.
        /// </summary>
        [MaxLength(20)]
        public string? AchievementIcon { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the user unlocked this achievement.
        /// </summary>
        [Required]
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the achievement record was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Entity representing a historical record of points changes for a user.
    /// Maps to the points_history table in the amesa_lottery schema.
    /// Provides an audit trail of all point awards and deductions.
    /// </summary>
    [Table("points_history", Schema = "amesa_lottery")]
    public class PointsHistory
    {
        /// <summary>
        /// Gets or sets the unique identifier of the points history record (primary key).
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user whose points changed.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the change in points (positive for awards, negative for deductions).
        /// </summary>
        [Required]
        public int PointsChange { get; set; }

        /// <summary>
        /// Gets or sets the reason for the points change (e.g., "Ticket Purchase", "Lottery Win", "Promotion Bonus").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional reference identifier (e.g., ticket ID, draw ID, transaction ID).
        /// Links the points change to a specific event or entity.
        /// </summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the points change occurred.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
