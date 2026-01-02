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

public class GamificationServiceGetUserGamificationTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceGetUserGamificationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldReturnDefault_WhenNoRecord()
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
        result.LongestStreak.Should().Be(0);
        result.LastEntryDate.Should().BeNull();
        result.RecentAchievements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldReturnData_WhenRecordExists()
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
        result.CurrentStreak.Should().Be(10);
        result.LongestStreak.Should().Be(15);
        result.LastEntryDate.Should().NotBeNull();
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
        result.RecentAchievements.Should().HaveCount(10); // Limited to 10
        result.RecentAchievements.Should().BeInDescendingOrder(a => a.UnlockedAt);
        result.RecentAchievements.First().Name.Should().Be("Achievement 1"); // Most recent
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldConvertDateOnlyToDateTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 100,
            CurrentLevel = 2,
            CurrentTier = "Bronze",
            CurrentStreak = 5,
            LongestStreak = 5,
            LastEntryDate = dateOnly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.LastEntryDate.Should().NotBeNull();
        result.LastEntryDate!.Value.Date.Should().Be(dateOnly.ToDateTime(TimeOnly.MinValue).Date);
    }

    [Fact]
    public async Task GetUserGamificationAsync_ShouldHandleNullLastEntryDate()
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
            LastEntryDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserGamificationAsync(userId);

        // Assert
        result.LastEntryDate.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
