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

public class GamificationServiceExtendedTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceExtendedTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldCreateNewRecord_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var gamification = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.CurrentStreak.Should().Be(1);
        gamification.LongestStreak.Should().Be(1);
        gamification.LastEntryDate.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldIncrementStreak_WhenConsecutiveDay()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var existingGamification = new UserGamification
        {
            UserId = userId,
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
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(6);
        updated.LongestStreak.Should().Be(6);
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldUpdateLongestStreak_WhenNewStreakIsLonger()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var existingGamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 10,
            LongestStreak = 5, // Current streak is longer than longest
            LastEntryDate = yesterday,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(existingGamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(11);
        updated.LongestStreak.Should().Be(11);
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldNotChangeStreak_WhenAlreadyEnteredToday()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingGamification = new UserGamification
        {
            UserId = userId,
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
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(5); // Should remain unchanged
        updated.LongestStreak.Should().Be(5);
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldResetStreak_WhenStreakIsBroken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var existingGamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 5,
            LongestStreak = 5,
            LastEntryDate = twoDaysAgo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(existingGamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(1); // Should reset to 1
        updated.LongestStreak.Should().Be(5); // Longest should remain unchanged
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockFirstEntryAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().Contain(a => a.Name == "First Entry");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockLuckyNumberAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 7).Select(i => new LotteryTicket
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
        achievements.Should().Contain(a => a.Name == "Lucky Number");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockWinnerAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Winner,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "Win");

        // Assert
        achievements.Should().Contain(a => a.Name == "Winner");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockOnFireAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
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
        var achievements = await _service.CheckAchievementsAsync(userId, "StreakUpdate");

        // Assert
        achievements.Should().Contain(a => a.Name == "On Fire");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockBigSpenderAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 10).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            PurchasePrice = 100, // Total: 1000
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().Contain(a => a.Name == "Big Spender");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldNotUnlockDuplicateAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingAchievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = "EntryBased",
            AchievementName = "First Entry",
            AchievementIcon = "🎟️",
            UnlockedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserAchievements.Add(existingAchievement);
        
        var ticket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().NotContain(a => a.Name == "First Entry");
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldReturnDefaultValues_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId.ToString());
        result.TotalPoints.Should().Be(0);
        result.CurrentLevel.Should().Be(1);
        result.CurrentTier.Should().Be("Bronze");
        result.CurrentStreak.Should().Be(0);
        result.RecentAchievements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldReturnGamificationWithAchievements()
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
        
        var achievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = "EntryBased",
            AchievementName = "First Entry",
            AchievementIcon = "🎟️",
            UnlockedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserAchievements.Add(achievement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalPoints.Should().Be(1000);
        result.CurrentLevel.Should().Be(5);
        result.CurrentTier.Should().Be("Silver");
        result.CurrentStreak.Should().Be(10);
        result.LongestStreak.Should().Be(15);
        result.RecentAchievements.Should().HaveCount(1);
        result.RecentAchievements.First().Name.Should().Be("First Entry");
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnEmptyList_WhenUserHasNoAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnAllAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievements = new List<UserAchievement>
        {
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "EntryBased",
                AchievementName = "First Entry",
                AchievementIcon = "🎟️",
                UnlockedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
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
            }
        };
        _context.UserAchievements.AddRange(achievements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(a => a.UnlockedAt);
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldReturnCorrectLevel()
    {
        // Act & Assert
        (await _service.CalculateLevelAsync(0)).Should().Be(1);
        (await _service.CalculateLevelAsync(100)).Should().Be(2);
        (await _service.CalculateLevelAsync(400)).Should().Be(3);
        (await _service.CalculateLevelAsync(10000)).Should().Be(11);
        (await _service.CalculateLevelAsync(1000000)).Should().Be(100); // Max level
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnCorrectTier()
    {
        // Act & Assert
        (await _service.CalculateTierAsync(0)).Should().Be("Bronze");
        (await _service.CalculateTierAsync(500)).Should().Be("Bronze");
        (await _service.CalculateTierAsync(501)).Should().Be("Silver");
        (await _service.CalculateTierAsync(2000)).Should().Be("Silver");
        (await _service.CalculateTierAsync(2001)).Should().Be("Gold");
        (await _service.CalculateTierAsync(5000)).Should().Be("Gold");
        (await _service.CalculateTierAsync(5001)).Should().Be("Platinum");
        (await _service.CalculateTierAsync(10000)).Should().Be("Platinum");
        (await _service.CalculateTierAsync(10001)).Should().Be("Diamond");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
