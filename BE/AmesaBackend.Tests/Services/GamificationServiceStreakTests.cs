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

public class GamificationServiceStreakTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceStreakTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldStartStreak_WhenNoPreviousEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        // Don't create a gamification record - let the service create it

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(1);
        updated.LongestStreak.Should().Be(1);
        updated.LastEntryDate.Should().NotBeNull();
        updated.LastEntryDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldIncrementStreak_WhenEntryOnSameDay()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 5,
            LongestStreak = 5,
            LastEntryDate = DateOnly.FromDateTime(today),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(5); // Should not increment on same day
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldIncrementStreak_WhenEntryOnNextDay()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 5,
            LongestStreak = 5,
            LastEntryDate = DateOnly.FromDateTime(yesterday),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(6); // Should increment
        updated.LongestStreak.Should().Be(6); // Should update longest streak
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldResetStreak_WhenEntryAfterGap()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2);
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 5,
            LongestStreak = 5,
            LastEntryDate = DateOnly.FromDateTime(twoDaysAgo),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(1); // Should reset to 1
        updated.LongestStreak.Should().Be(5); // Should keep longest streak
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldUpdateLongestStreak_WhenCurrentExceedsLongest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 9,
            LongestStreak = 9,
            LastEntryDate = DateOnly.FromDateTime(yesterday),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(10);
        updated.LongestStreak.Should().Be(10); // Should update longest streak
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldCreateGamification_WhenNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var created = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        created.Should().NotBeNull();
        created!.CurrentStreak.Should().Be(1);
        created.LongestStreak.Should().Be(1);
    }

    [Fact]
    public async Task UpdateStreakAsync_ShouldHandleMultipleDaysGap()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fiveDaysAgo = DateTime.UtcNow.Date.AddDays(-5);
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 10,
            LongestStreak = 10,
            LastEntryDate = DateOnly.FromDateTime(fiveDaysAgo),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserGamification.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateStreakAsync(userId);

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentStreak.Should().Be(1); // Should reset after gap
        updated.LongestStreak.Should().Be(10); // Should keep longest streak
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
