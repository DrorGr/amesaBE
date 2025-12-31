using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Lottery.Models
{
    [Table("user_gamification", Schema = "amesa_lottery")]
    public class UserGamification
    {
        [Key]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("total_points")]
        public int TotalPoints { get; set; } = 0;

        [Required]
        [Column("current_level")]
        public int CurrentLevel { get; set; } = 1;

        [Required]
        [MaxLength(20)]
        [Column("current_tier")]
        public string CurrentTier { get; set; } = "Bronze";

        [Required]
        [Column("current_streak")]
        public int CurrentStreak { get; set; } = 0;

        [Required]
        [Column("longest_streak")]
        public int LongestStreak { get; set; } = 0;

        [Column("last_entry_date")]
        public DateOnly? LastEntryDate { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("user_achievements", Schema = "amesa_lottery")]
    public class UserAchievement
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("achievement_type")]
        public string AchievementType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("achievement_name")]
        public string AchievementName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("achievement_icon")]
        public string? AchievementIcon { get; set; }

        [Required]
        [Column("unlocked_at")]
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("points_history", Schema = "amesa_lottery")]
    public class PointsHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("points_change")]
        public int PointsChange { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

        [Column("reference_id")]
        public Guid? ReferenceId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}



