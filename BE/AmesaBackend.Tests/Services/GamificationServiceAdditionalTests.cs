using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class GamificationServiceAdditionalTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceAdditionalTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateLevel_WhenPointsIncrease()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 100, // Level 2
            CurrentLevel = 2,
            CurrentTier = "Bronze",
            CurrentStreak = 0,
            LongestStreak = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.AwardPointsAsync(userId, 400, "Level up"); // Total: 500, should be level 3

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentLevel.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateTier_WhenPointsIncrease()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 500, // Bronze
            CurrentLevel = 3,
            CurrentTier = "Bronze",
            CurrentStreak = 0,
            LongestStreak = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.AwardPointsAsync(userId, 500, "Tier up"); // Total: 1000, should be Silver

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentTier.Should().Be("Silver");
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldHandleNegativePoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 100,
            CurrentLevel = 2,
            CurrentTier = "Bronze",
            CurrentStreak = 0,
            LongestStreak = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.AwardPointsAsync(userId, -50, "Penalty");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.TotalPoints.Should().Be(50);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldPreventNegativeTotalPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 50,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 0,
            LongestStreak = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.AwardPointsAsync(userId, -100, "Large Penalty");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.TotalPoints.Should().Be(0); // Should not go negative
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockLotteryLoverAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 25).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().Contain(a => a.Name == "Lottery Lover");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockHighRollerAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 100).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().Contain(a => a.Name == "High Roller");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockLuckyStarAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 10).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = i <= 3 ? TicketStatus.Winner : TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "Win");

        // Assert
        achievements.Should().Contain(a => a.Name == "Lucky Star");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockChampionAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 20).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = i <= 10 ? TicketStatus.Winner : TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "Win");

        // Assert
        achievements.Should().Contain(a => a.Name == "Champion");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockUnstoppableAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 30,
            LongestStreak = 30,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "StreakUpdate");

        // Assert
        achievements.Should().Contain(a => a.Name == "Unstoppable");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockRainbowAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 100,
            LongestStreak = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "StreakUpdate");

        // Assert
        achievements.Should().Contain(a => a.Name == "Rainbow");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockSharpshooterAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 20).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = i <= 5 ? TicketStatus.Winner : TicketStatus.Active, // 25% win rate
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().Contain(a => a.Name == "Sharpshooter");
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldReturnRecentAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 1000,
            CurrentLevel = 5,
            CurrentTier = "Silver",
            CurrentStreak = 10,
            LongestStreak = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        
        var achievements = new List<UserAchievement>
        {
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "EntryBased",
                AchievementName = "First Entry",
                AchievementIcon = "🎟️",
                UnlockedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "WinBased",
                AchievementName = "Winner",
                AchievementIcon = "🏆",
                UnlockedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "StreakBased",
                AchievementName = "On Fire",
                AchievementIcon = "🔥",
                UnlockedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.UserAchievements.AddRange(achievements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.RecentAchievements.Should().HaveCount(3);
        result.RecentAchievements.Should().BeInDescendingOrder(a => a.UnlockedAt);
        result.RecentAchievements.First().Name.Should().Be("On Fire"); // Most recent
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldLimitRecentAchievementsTo10()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 1000,
            CurrentLevel = 5,
            CurrentTier = "Silver",
            CurrentStreak = 10,
            LongestStreak = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        
        var achievements = Enumerable.Range(1, 15).Select(i => new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = "EntryBased",
            AchievementName = $"Achievement {i}",
            AchievementIcon = "🎟️",
            UnlockedAt = DateTime.UtcNow.AddDays(-i),
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();
        _context.UserAchievements.AddRange(achievements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.RecentAchievements.Should().HaveCount(10); // Should be limited to 10
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
