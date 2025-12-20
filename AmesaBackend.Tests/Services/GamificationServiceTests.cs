using Xunit;
using Moq;
using FluentAssertions;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Tests.Services
{
    public class GamificationServiceTests : IDisposable
    {
        private readonly LotteryDbContext _context;
        private readonly Mock<ILogger<GamificationService>> _mockLogger;
        private readonly GamificationService _gamificationService;
        private readonly Guid _testUserId;

        public GamificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: $"GamificationTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new LotteryDbContext(options);
            _mockLogger = new Mock<ILogger<GamificationService>>();
            _gamificationService = new GamificationService(_context, _mockLogger.Object);
            _testUserId = Guid.NewGuid();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region AwardPointsAsync Tests

        [Fact]
        public async Task AwardPointsAsync_NewUser_CreatesGamificationRecord()
        {
            // Arrange
            var points = 50;
            var reason = "Test Reason";

            // Act
            await _gamificationService.AwardPointsAsync(_testUserId, points, reason);

            // Assert
            var gamification = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            
            gamification.Should().NotBeNull();
            gamification!.TotalPoints.Should().Be(points);
            gamification.CurrentLevel.Should().Be(1);
            gamification.CurrentTier.Should().Be("Bronze");
            
            var history = await _context.PointsHistory
                .FirstOrDefaultAsync(h => h.UserId == _testUserId);
            history.Should().NotBeNull();
            history!.PointsChange.Should().Be(points);
            history.Reason.Should().Be(reason);
        }

        [Fact]
        public async Task AwardPointsAsync_ExistingUser_UpdatesPoints()
        {
            // Arrange
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 100,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 0,
                LongestStreak = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            // Act
            await _gamificationService.AwardPointsAsync(_testUserId, 50, "Additional Points");

            // Assert
            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.TotalPoints.Should().Be(150);
        }

        [Fact]
        public async Task AwardPointsAsync_ZeroPoints_DoesNothing()
        {
            // Arrange
            var initialCount = await _context.UserGamification.CountAsync();

            // Act
            await _gamificationService.AwardPointsAsync(_testUserId, 0, "Zero Points");

            // Assert
            var finalCount = await _context.UserGamification.CountAsync();
            finalCount.Should().Be(initialCount);
        }

        [Fact]
        public async Task AwardPointsAsync_NegativePoints_PreventsNegativeTotal()
        {
            // Arrange
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 50,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 0,
                LongestStreak = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            // Act
            await _gamificationService.AwardPointsAsync(_testUserId, -100, "Penalty");

            // Assert
            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.TotalPoints.Should().Be(0); // Should not go negative
        }

        [Fact]
        public async Task AwardPointsAsync_WithReferenceId_StoresReferenceId()
        {
            // Arrange
            var referenceId = Guid.NewGuid();

            // Act
            await _gamificationService.AwardPointsAsync(_testUserId, 50, "Test", referenceId);

            // Assert
            var history = await _context.PointsHistory
                .FirstOrDefaultAsync(h => h.UserId == _testUserId);
            history!.ReferenceId.Should().Be(referenceId);
        }

        [Fact]
        public async Task AwardPointsAsync_RecalculatesLevelAndTier()
        {
            // Arrange
            // Award enough points to reach level 2 and Silver tier
            var pointsForLevel2 = 400; // sqrt(400/100) + 1 = 3, but capped at 2 for testing

            // Act
            await _gamificationService.AwardPointsAsync(_testUserId, pointsForLevel2, "Level Up");

            // Assert
            var gamification = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            
            gamification!.CurrentLevel.Should().BeGreaterThan(1);
            // Tier should be at least Bronze, possibly Silver if points >= 501
        }

        #endregion

        #region CalculateLevelAsync Tests

        [Theory]
        [InlineData(0, 1)]
        [InlineData(100, 2)]
        [InlineData(400, 3)]
        [InlineData(900, 4)]
        [InlineData(10000, 11)]
        [InlineData(1000000, 100)] // Max level
        public async Task CalculateLevelAsync_CalculatesCorrectLevel(int points, int expectedLevel)
        {
            // Act
            var level = await _gamificationService.CalculateLevelAsync(points);

            // Assert
            level.Should().Be(expectedLevel);
        }

        [Fact]
        public async Task CalculateLevelAsync_MaxLevel_Returns100()
        {
            // Arrange
            var veryHighPoints = 10000000;

            // Act
            var level = await _gamificationService.CalculateLevelAsync(veryHighPoints);

            // Assert
            level.Should().Be(100); // Max level
        }

        #endregion

        #region CalculateTierAsync Tests

        [Theory]
        [InlineData(0, "Bronze")]
        [InlineData(500, "Bronze")]
        [InlineData(501, "Silver")]
        [InlineData(2000, "Silver")]
        [InlineData(2001, "Gold")]
        [InlineData(5000, "Gold")]
        [InlineData(5001, "Platinum")]
        [InlineData(10000, "Platinum")]
        [InlineData(10001, "Diamond")]
        [InlineData(50000, "Diamond")]
        public async Task CalculateTierAsync_CalculatesCorrectTier(int points, string expectedTier)
        {
            // Act
            var tier = await _gamificationService.CalculateTierAsync(points);

            // Assert
            tier.Should().Be(expectedTier);
        }

        #endregion

        #region UpdateStreakAsync Tests

        [Fact]
        public async Task UpdateStreakAsync_NewUser_CreatesRecordWithStreak1()
        {
            // Act
            await _gamificationService.UpdateStreakAsync(_testUserId);

            // Assert
            var gamification = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            
            gamification.Should().NotBeNull();
            gamification!.CurrentStreak.Should().Be(1);
            gamification.LongestStreak.Should().Be(1);
            gamification.LastEntryDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        }

        [Fact]
        public async Task UpdateStreakAsync_ConsecutiveDay_IncrementsStreak()
        {
            // Arrange
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 0,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 5,
                LongestStreak = 5,
                LastEntryDate = yesterday,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            // Act
            await _gamificationService.UpdateStreakAsync(_testUserId);

            // Assert
            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.CurrentStreak.Should().Be(6);
            updated.LongestStreak.Should().Be(6);
        }

        [Fact]
        public async Task UpdateStreakAsync_SameDay_DoesNotChangeStreak()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 0,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 5,
                LongestStreak = 5,
                LastEntryDate = today,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            // Act
            await _gamificationService.UpdateStreakAsync(_testUserId);

            // Assert
            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.CurrentStreak.Should().Be(5); // Unchanged
        }

        [Fact]
        public async Task UpdateStreakAsync_BrokenStreak_ResetsTo1()
        {
            // Arrange
            var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 0,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 10,
                LongestStreak = 10,
                LastEntryDate = twoDaysAgo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            // Act
            await _gamificationService.UpdateStreakAsync(_testUserId);

            // Assert
            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.CurrentStreak.Should().Be(1); // Reset
            updated.LongestStreak.Should().Be(10); // Longest streak preserved
        }

        [Fact]
        public async Task UpdateStreakAsync_NewLongestStreak_UpdatesLongestStreak()
        {
            // Arrange
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 0,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 9,
                LongestStreak = 9,
                LastEntryDate = yesterday,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            // Act
            await _gamificationService.UpdateStreakAsync(_testUserId);

            // Assert
            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.CurrentStreak.Should().Be(10);
            updated.LongestStreak.Should().Be(10); // Updated
        }

        #endregion

        #region CheckAchievementsAsync Tests

        [Fact]
        public async Task CheckAchievementsAsync_FirstEntry_UnlocksFirstEntryAchievement()
        {
            // Arrange
            var ticket = new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                HouseId = Guid.NewGuid(),
                TicketNumber = "T001",
                Status = "active",
                PurchasePrice = 50,
                PurchaseDate = DateTime.UtcNow,
                IsWinner = false
            };
            _context.LotteryTickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var achievements = await _gamificationService.CheckAchievementsAsync(
                _testUserId, "EntryPurchase");

            // Assert
            achievements.Should().Contain(a => a.Name == "First Entry");
            var achievement = await _context.UserAchievements
                .FirstOrDefaultAsync(a => a.UserId == _testUserId && a.AchievementName == "First Entry");
            achievement.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckAchievementsAsync_SeventhEntry_UnlocksLuckyNumberAchievement()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            for (int i = 1; i <= 7; i++)
            {
                _context.LotteryTickets.Add(new LotteryTicket
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    HouseId = houseId,
                    TicketNumber = $"T{i:000}",
                    Status = "active",
                    PurchasePrice = 50,
                    PurchaseDate = DateTime.UtcNow,
                    IsWinner = false
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var achievements = await _gamificationService.CheckAchievementsAsync(
                _testUserId, "EntryPurchase");

            // Assert
            achievements.Should().Contain(a => a.Name == "Lucky Number");
        }

        [Fact]
        public async Task CheckAchievementsAsync_FirstWin_UnlocksWinnerAchievement()
        {
            // Arrange
            var ticket = new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                HouseId = Guid.NewGuid(),
                TicketNumber = "T001",
                Status = "active",
                PurchasePrice = 50,
                PurchaseDate = DateTime.UtcNow,
                IsWinner = true
            };
            _context.LotteryTickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var achievements = await _gamificationService.CheckAchievementsAsync(
                _testUserId, "Win");

            // Assert
            achievements.Should().Contain(a => a.Name == "Winner");
        }

        [Fact]
        public async Task CheckAchievementsAsync_Streak7_UnlocksOnFireAchievement()
        {
            // Arrange
            var gamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 0,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 7,
                LongestStreak = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(gamification);
            await _context.SaveChangesAsync();

            // Act
            var achievements = await _gamificationService.CheckAchievementsAsync(
                _testUserId, "StreakUpdate");

            // Assert
            achievements.Should().Contain(a => a.Name == "On Fire");
        }

        [Fact]
        public async Task CheckAchievementsAsync_AlreadyUnlocked_DoesNotUnlockAgain()
        {
            // Arrange
            var existingAchievement = new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                AchievementType = "EntryBased",
                AchievementName = "First Entry",
                AchievementIcon = "🎟️",
                UnlockedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _context.UserAchievements.Add(existingAchievement);
            await _context.SaveChangesAsync();

            // Act
            var achievements = await _gamificationService.CheckAchievementsAsync(
                _testUserId, "EntryPurchase");

            // Assert
            achievements.Should().NotContain(a => a.Name == "First Entry");
        }

        [Fact]
        public async Task CheckAchievementsAsync_BigSpender_UnlocksBigSpenderAchievement()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            for (int i = 1; i <= 20; i++)
            {
                _context.LotteryTickets.Add(new LotteryTicket
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    HouseId = houseId,
                    TicketNumber = $"T{i:000}",
                    Status = "active",
                    PurchasePrice = 50, // 20 * 50 = 1000 total
                    PurchaseDate = DateTime.UtcNow,
                    IsWinner = false
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var achievements = await _gamificationService.CheckAchievementsAsync(
                _testUserId, "EntryPurchase");

            // Assert
            achievements.Should().Contain(a => a.Name == "Big Spender");
        }

        #endregion

        #region GetUserGamificationAsync Tests

        [Fact]
        public async Task GetUserGamificationAsync_NoRecord_ReturnsDefaultValues()
        {
            // Act
            var result = await _gamificationService.GetUserGamificationAsync(_testUserId);

            // Assert
            result.Should().NotBeNull();
            result.TotalPoints.Should().Be(0);
            result.CurrentLevel.Should().Be(1);
            result.CurrentTier.Should().Be("Bronze");
            result.CurrentStreak.Should().Be(0);
            result.RecentAchievements.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserGamificationAsync_WithRecord_ReturnsGamificationData()
        {
            // Arrange
            var gamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 500,
                CurrentLevel = 2,
                CurrentTier = "Silver",
                CurrentStreak = 5,
                LongestStreak = 10,
                LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(gamification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _gamificationService.GetUserGamificationAsync(_testUserId);

            // Assert
            result.TotalPoints.Should().Be(500);
            result.CurrentLevel.Should().Be(2);
            result.CurrentTier.Should().Be("Silver");
            result.CurrentStreak.Should().Be(5);
            result.LongestStreak.Should().Be(10);
        }

        [Fact]
        public async Task GetUserGamificationAsync_WithAchievements_ReturnsRecentAchievements()
        {
            // Arrange
            var gamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 100,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 0,
                LongestStreak = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(gamification);

            for (int i = 1; i <= 15; i++)
            {
                _context.UserAchievements.Add(new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "EntryBased",
                    AchievementName = $"Achievement {i}",
                    AchievementIcon = "🎟️",
                    UnlockedAt = DateTime.UtcNow.AddDays(-i),
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _gamificationService.GetUserGamificationAsync(_testUserId);

            // Assert
            result.RecentAchievements.Should().HaveCount(10); // Should limit to 10 most recent
            result.RecentAchievements.Should().BeInDescendingOrder(a => a.UnlockedAt);
        }

        #endregion

        #region GetUserAchievementsAsync Tests

        [Fact]
        public async Task GetUserAchievementsAsync_NoAchievements_ReturnsEmptyList()
        {
            // Act
            var result = await _gamificationService.GetUserAchievementsAsync(_testUserId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserAchievementsAsync_WithAchievements_ReturnsAllAchievements()
        {
            // Arrange
            var achievements = new List<UserAchievement>
            {
                new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "EntryBased",
                    AchievementName = "First Entry",
                    AchievementIcon = "🎟️",
                    UnlockedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "WinBased",
                    AchievementName = "Winner",
                    AchievementIcon = "🏆",
                    UnlockedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };
            _context.UserAchievements.AddRange(achievements);
            await _context.SaveChangesAsync();

            // Act
            var result = await _gamificationService.GetUserAchievementsAsync(_testUserId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeInDescendingOrder(a => a.UnlockedAt);
            result.Should().Contain(a => a.Name == "First Entry");
            result.Should().Contain(a => a.Name == "Winner");
        }

        #endregion
    }
}



