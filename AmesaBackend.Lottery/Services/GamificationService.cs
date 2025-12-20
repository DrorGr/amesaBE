using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services
{
    /// <summary>
    /// Gamification service implementation
    /// Handles points, levels, tiers, streaks, and achievements
    /// </summary>
    public class GamificationService : IGamificationService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<GamificationService> _logger;

        public GamificationService(
            LotteryDbContext context,
            ILogger<GamificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AwardPointsAsync(Guid userId, int points, string reason, Guid? referenceId = null)
        {
            if (points == 0)
            {
                return; // No points to award
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get or create user gamification record
                var gamification = await _context.UserGamification
                    .FirstOrDefaultAsync(g => g.UserId == userId);

                if (gamification == null)
                {
                    gamification = new UserGamification
                    {
                        UserId = userId,
                        TotalPoints = 0,
                        CurrentLevel = 1,
                        CurrentTier = "Bronze",
                        CurrentStreak = 0,
                        LongestStreak = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserGamification.Add(gamification);
                }

                // Update points
                gamification.TotalPoints += points;
                if (gamification.TotalPoints < 0)
                {
                    gamification.TotalPoints = 0; // Prevent negative points
                }

                // Recalculate level and tier
                gamification.CurrentLevel = await CalculateLevelAsync(gamification.TotalPoints);
                gamification.CurrentTier = await CalculateTierAsync(gamification.TotalPoints);
                gamification.UpdatedAt = DateTime.UtcNow;

                // Record in points history
                var historyEntry = new PointsHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PointsChange = points,
                    Reason = reason,
                    ReferenceId = referenceId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PointsHistory.Add(historyEntry);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Awarded {Points} points to user {UserId} for reason: {Reason}",
                    points, userId, reason);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to award points to user {UserId}", userId);
                throw;
            }
        }

        public Task<int> CalculateLevelAsync(int points)
        {
            // Formula: floor(sqrt(total_points / 100)) + 1 (max 100)
            var level = (int)Math.Floor(Math.Sqrt(points / 100.0)) + 1;
            var maxLevel = 100;
            return Task.FromResult(Math.Min(level, maxLevel));
        }

        public Task<string> CalculateTierAsync(int points)
        {
            // Tiers based on points:
            // Bronze: 0-500
            // Silver: 501-2,000
            // Gold: 2,001-5,000
            // Platinum: 5,001-10,000
            // Diamond: 10,001+
            if (points <= 500)
                return Task.FromResult("Bronze");
            if (points <= 2000)
                return Task.FromResult("Silver");
            if (points <= 5000)
                return Task.FromResult("Gold");
            if (points <= 10000)
                return Task.FromResult("Platinum");
            return Task.FromResult("Diamond");
        }

        public async Task UpdateStreakAsync(Guid userId)
        {
            try
            {
                var gamification = await _context.UserGamification
                    .FirstOrDefaultAsync(g => g.UserId == userId);

                if (gamification == null)
                {
                    // Create gamification record if it doesn't exist
                    gamification = new UserGamification
                    {
                        UserId = userId,
                        TotalPoints = 0,
                        CurrentLevel = 1,
                        CurrentTier = "Bronze",
                        CurrentStreak = 1,
                        LongestStreak = 1,
                        LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserGamification.Add(gamification);
                }
                else
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var yesterday = today.AddDays(-1);

                    if (gamification.LastEntryDate == yesterday)
                    {
                        // Consecutive day - increment streak
                        gamification.CurrentStreak++;
                        if (gamification.CurrentStreak > gamification.LongestStreak)
                        {
                            gamification.LongestStreak = gamification.CurrentStreak;
                        }
                    }
                    else if (gamification.LastEntryDate == today)
                    {
                        // Already entered today - no change
                    }
                    else
                    {
                        // Streak broken - reset to 1
                        gamification.CurrentStreak = 1;
                    }

                    gamification.LastEntryDate = today;
                    gamification.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update streak for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<AchievementDto>> CheckAchievementsAsync(Guid userId, string actionType, object? actionData = null)
        {
            var newlyUnlocked = new List<AchievementDto>();

            try
            {
                // Get user gamification data
                var gamification = await GetUserGamificationAsync(userId);
                
                // Get existing achievements
                var existingAchievements = await _context.UserAchievements
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.AchievementType, a.AchievementName })
                    .ToListAsync();

                // Get user statistics for achievement checking
                var tickets = await _context.LotteryTickets
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                var totalEntries = tickets.Count;
                var totalWins = tickets.Count(t => t.IsWinner);
                var totalSpending = tickets.Sum(t => t.PurchasePrice);
                var winRate = totalEntries > 0 ? (decimal)totalWins / totalEntries : 0;

                // Define achievement rules
                var achievementsToCheck = new List<(string Type, string Name, string Icon, Func<bool> Check)>();

                // Entry-based achievements
                if (actionType == "EntryPurchase")
                {
                    achievementsToCheck.AddRange(new (string Type, string Name, string Icon, Func<bool> Check)[]
                    {
                        ("EntryBased", "First Entry", "🎟️", () => totalEntries == 1 && !existingAchievements.Any(a => a.AchievementType == "EntryBased" && a.AchievementName == "First Entry")),
                        ("EntryBased", "Lucky Number", "🎫", () => totalEntries == 7 && !existingAchievements.Any(a => a.AchievementType == "EntryBased" && a.AchievementName == "Lucky Number")),
                        ("EntryBased", "Lottery Lover", "🎪", () => totalEntries == 25 && !existingAchievements.Any(a => a.AchievementType == "EntryBased" && a.AchievementName == "Lottery Lover")),
                        ("EntryBased", "High Roller", "🎰", () => totalEntries == 100 && !existingAchievements.Any(a => a.AchievementType == "EntryBased" && a.AchievementName == "High Roller"))
                    });
                }

                // Win-based achievements
                if (actionType == "Win")
                {
                    achievementsToCheck.AddRange(new (string Type, string Name, string Icon, Func<bool> Check)[]
                    {
                        ("WinBased", "Winner", "🏆", () => totalWins == 1 && !existingAchievements.Any(a => a.AchievementType == "WinBased" && a.AchievementName == "Winner")),
                        ("WinBased", "Lucky Star", "🌟", () => totalWins == 3 && !existingAchievements.Any(a => a.AchievementType == "WinBased" && a.AchievementName == "Lucky Star")),
                        ("WinBased", "Champion", "👑", () => totalWins == 10 && !existingAchievements.Any(a => a.AchievementType == "WinBased" && a.AchievementName == "Champion")),
                        ("WinBased", "Legend", "💎", () => totalWins == 25 && !existingAchievements.Any(a => a.AchievementType == "WinBased" && a.AchievementName == "Legend"))
                    });
                }

                // Streak-based achievements
                if (actionType == "EntryPurchase" || actionType == "StreakUpdate")
                {
                    achievementsToCheck.AddRange(new (string Type, string Name, string Icon, Func<bool> Check)[]
                    {
                        ("StreakBased", "On Fire", "🔥", () => gamification.CurrentStreak == 7 && !existingAchievements.Any(a => a.AchievementType == "StreakBased" && a.AchievementName == "On Fire")),
                        ("StreakBased", "Unstoppable", "⚡", () => gamification.CurrentStreak == 30 && !existingAchievements.Any(a => a.AchievementType == "StreakBased" && a.AchievementName == "Unstoppable")),
                        ("StreakBased", "Rainbow", "🌈", () => gamification.CurrentStreak == 100 && !existingAchievements.Any(a => a.AchievementType == "StreakBased" && a.AchievementName == "Rainbow"))
                    });
                }

                // Special achievements
                achievementsToCheck.AddRange(new (string Type, string Name, string Icon, Func<bool> Check)[]
                {
                    ("Special", "Big Spender", "💰", () => totalSpending >= 1000 && !existingAchievements.Any(a => a.AchievementType == "Special" && a.AchievementName == "Big Spender")),
                    ("Special", "Sharpshooter", "🎯", () => totalEntries >= 10 && winRate >= 0.20m && !existingAchievements.Any(a => a.AchievementType == "Special" && a.AchievementName == "Sharpshooter"))
                });

                // Check and unlock achievements
                foreach (var (type, name, icon, check) in achievementsToCheck)
                {
                    if (check())
                    {
                        var achievement = new UserAchievement
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            AchievementType = type,
                            AchievementName = name,
                            AchievementIcon = icon,
                            UnlockedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.UserAchievements.Add(achievement);

                        newlyUnlocked.Add(new AchievementDto
                        {
                            Id = achievement.Id.ToString(),
                            Type = type,
                            Name = name,
                            Description = $"Unlocked {name} achievement",
                            Icon = icon,
                            UnlockedAt = DateTime.UtcNow,
                            Category = type
                        });

                        _logger.LogInformation("Achievement unlocked: {Name} for user {UserId}", name, userId);
                    }
                }

                if (newlyUnlocked.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check achievements for user {UserId}", userId);
                throw;
            }

            return newlyUnlocked;
        }

        public async Task<UserGamificationDto> GetUserGamificationAsync(Guid userId)
        {
            try
            {
                var gamification = await _context.UserGamification
                    .FirstOrDefaultAsync(g => g.UserId == userId);

                if (gamification == null)
                {
                    // Return default values if no gamification record exists
                    return new UserGamificationDto
                    {
                        UserId = userId.ToString(),
                        TotalPoints = 0,
                        CurrentLevel = 1,
                        CurrentTier = "Bronze",
                        CurrentStreak = 0,
                        LongestStreak = 0,
                        LastEntryDate = null,
                        RecentAchievements = new List<AchievementDto>()
                    };
                }

                // Get recent achievements
                var achievements = await _context.UserAchievements
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.UnlockedAt)
                    .Take(10)
                    .Select(a => new AchievementDto
                    {
                        Id = a.Id.ToString(),
                        Name = a.AchievementName,
                        Description = $"Unlocked {a.AchievementName} achievement",
                        Icon = a.AchievementIcon,
                        UnlockedAt = a.UnlockedAt,
                        Category = a.AchievementType
                    })
                    .ToListAsync();

                return new UserGamificationDto
                {
                    UserId = userId.ToString(),
                    TotalPoints = gamification.TotalPoints,
                    CurrentLevel = gamification.CurrentLevel,
                    CurrentTier = gamification.CurrentTier,
                    CurrentStreak = gamification.CurrentStreak,
                    LongestStreak = gamification.LongestStreak,
                    RecentAchievements = achievements
                };
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving gamification data for user {UserId}", userId);
                // Return default values on database error to allow service to continue
                return new UserGamificationDto
                {
                    UserId = userId.ToString(),
                    TotalPoints = 0,
                    CurrentLevel = 1,
                    CurrentTier = "Bronze",
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    LastEntryDate = null,
                    RecentAchievements = new List<AchievementDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gamification data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<AchievementDto>> GetUserAchievementsAsync(Guid userId)
        {
            try
            {
                var achievements = await _context.UserAchievements
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.UnlockedAt)
                    .Select(a => new AchievementDto
                    {
                        Id = a.Id.ToString(),
                        Name = a.AchievementName,
                        Description = $"Unlocked {a.AchievementName} achievement",
                        Icon = a.AchievementIcon,
                        UnlockedAt = a.UnlockedAt,
                        Category = a.AchievementType
                    })
                    .ToListAsync();

                return achievements;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving achievements for user {UserId}", userId);
                // Return empty list on database error
                return new List<AchievementDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving achievements for user {UserId}", userId);
                throw;
            }
        }
    }
}

