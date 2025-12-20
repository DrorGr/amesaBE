using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmesaBackend.Lottery.Models
{
    [Table("user_gamification", Schema = "amesa_lottery")]
    public class UserGamification
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        public int TotalPoints { get; set; } = 0;

        [Required]
        public int CurrentLevel { get; set; } = 1;

        [Required]
        [MaxLength(20)]
        public string CurrentTier { get; set; } = "Bronze";

        [Required]
        public int CurrentStreak { get; set; } = 0;

        [Required]
        public int LongestStreak { get; set; } = 0;

        public DateOnly? LastEntryDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("user_achievements", Schema = "amesa_lottery")]
    public class UserAchievement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AchievementType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string AchievementName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? AchievementIcon { get; set; }

        [Required]
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("points_history", Schema = "amesa_lottery")]
    public class PointsHistory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int PointsChange { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        public Guid? ReferenceId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}



