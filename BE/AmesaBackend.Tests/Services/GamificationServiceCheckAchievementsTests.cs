using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class GamificationServiceCheckAchievementsTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceCheckAchievementsTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockFirstEntry()
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
    public async Task CheckAchievementsAsync_ShouldUnlockLuckyNumber()
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
    public async Task CheckAchievementsAsync_ShouldUnlockBigSpender()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 10).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = TicketStatus.Active,
            PurchasePrice = 100, // $100 each = $1000 total
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
    public async Task CheckAchievementsAsync_ShouldUnlockLegend()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 30).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = i <= 25 ? TicketStatus.Winner : TicketStatus.Active,
            PurchasePrice = 10,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "Win");

        // Assert
        achievements.Should().Contain(a => a.Name == "Legend");
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldNotUnlockAlreadyUnlockedAchievements()
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
            UnlockedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
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
        achievements.Should().NotContain(a => a.Name == "First Entry"); // Already unlocked
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldReturnEmptyList_WhenNoNewAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();
        // No tickets, no achievements

        // Act
        var achievements = await _service.CheckAchievementsAsync(userId, "EntryPurchase");

        // Assert
        achievements.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldHandleWinActionType()
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
    public async Task CheckAchievementsAsync_ShouldHandleStreakUpdateActionType()
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
    public async Task CheckAchievementsAsync_ShouldCalculateWinRateCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = Enumerable.Range(1, 20).Select(i => new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = Guid.NewGuid(),
            Status = i <= 4 ? TicketStatus.Winner : TicketStatus.Active, // 20% win rate
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
    public async Task CheckAchievementsAsync_ShouldHandleMultipleAchievements()
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
        // When checking with 25 entries, it should unlock Lottery Lover (25 entries)
        // But First Entry (1 entry) and Lucky Number (7 entries) were already eligible earlier
        // The service only checks achievements at the current count, so only Lottery Lover should be unlocked
        achievements.Should().Contain(a => a.Name == "Lottery Lover");
        // First Entry and Lucky Number would have been unlocked when those counts were reached
        // This test verifies that Lottery Lover is unlocked at 25 entries
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
