using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class GamificationServiceTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCreateNewGamificationRecord_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var points = 100;
        var reason = "Test reason";

        // Act
        await _service.AwardPointsAsync(userId, points, reason);

        // Assert
        var gamification = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.TotalPoints.Should().Be(points);
        gamification.CurrentLevel.Should().BeGreaterThan(0);
        
        var history = await _context.PointsHistory.FirstOrDefaultAsync(h => h.UserId == userId);
        history.Should().NotBeNull();
        history!.PointsChange.Should().Be(points);
        history.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateExistingGamificationRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingGamification = new UserGamification
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
        _context.UserGamification.Add(existingGamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.AwardPointsAsync(userId, 100, "Additional points");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.TotalPoints.Should().Be(150);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldNotAwardPoints_WhenPointsIsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 0, "No points");

        // Assert
        var gamification = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().BeNull();
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldPreventNegativePoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingGamification = new UserGamification
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
        _context.UserGamification.Add(existingGamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.AwardPointsAsync(userId, -100, "Deducting points");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.TotalPoints.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(100, 2)]
    [InlineData(400, 3)]
    [InlineData(900, 4)]
    [InlineData(10000, 11)]
    [InlineData(1000000, 100)] // Max level
    public async Task CalculateLevelAsync_ShouldReturnCorrectLevel(int points, int expectedLevel)
    {
        // Act
        var level = await _service.CalculateLevelAsync(points);

        // Assert
        level.Should().Be(expectedLevel);
    }

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
    public async Task CalculateTierAsync_ShouldReturnCorrectTier(int points, string expectedTier)
    {
        // Act
        var tier = await _service.CalculateTierAsync(points);

        // Assert
        tier.Should().Be(expectedTier);
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
        gamification.LastEntryDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
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
    public async Task UpdateStreakAsync_ShouldResetStreak_WhenNotConsecutive()
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
            LongestStreak = 10,
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
        updated!.CurrentStreak.Should().Be(1);
        updated.LongestStreak.Should().Be(10); // Should preserve longest streak
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
    public async Task GetUserGamificationAsync_ShouldReturnUserData_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 1000,
            CurrentLevel = 5,
            CurrentTier = "Silver",
            CurrentStreak = 3,
            LongestStreak = 5,
            LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId.ToString());
        result.TotalPoints.Should().Be(1000);
        result.CurrentLevel.Should().Be(5);
        result.CurrentTier.Should().Be("Silver");
        result.CurrentStreak.Should().Be(3);
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockFirstEntryAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var house = new AmesaBackend.Models.House
        {
            Id = houseId,
            Title = "Test House",
            Description = "Test",
            Price = 100,
            Status = AmesaBackend.Models.LotteryStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Houses.Add(house);
        
        var ticket = new AmesaBackend.Models.LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T001",
            PurchasePrice = 100,
            Status = AmesaBackend.Models.TicketStatus.Active,
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
    public async Task GetUserAchievementsAsync_ShouldReturnEmptyList_WhenNoAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var achievements = await _service.GetUserAchievementsAsync(userId);

        // Assert
        achievements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnUserAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();
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
        var achievements = await _service.GetUserAchievementsAsync(userId);

        // Assert
        achievements.Should().HaveCount(1);
        achievements[0].Name.Should().Be("First Entry");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
