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

public class GamificationServiceAwardPointsTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceAwardPointsTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCreateGamification_WhenNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 100, "Test reason");

        // Assert
        var gamification = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.TotalPoints.Should().Be(100);
        gamification.CurrentLevel.Should().Be(2); // 100 points = level 2
        gamification.CurrentTier.Should().Be("Bronze"); // Still Bronze at 100 points
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateExistingGamification()
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
        await _service.AwardPointsAsync(userId, 50, "Additional points");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.TotalPoints.Should().Be(100);
        updated.CurrentLevel.Should().Be(2); // Should update level
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCreatePointsHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reason = "Test reason";
        var referenceId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 100, reason, referenceId);

        // Assert
        var history = await _context.PointsHistory.FirstOrDefaultAsync(h => h.UserId == userId);
        history.Should().NotBeNull();
        history!.PointsChange.Should().Be(100);
        history.Reason.Should().Be(reason);
        history.ReferenceId.Should().Be(referenceId);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldNotCreateRecord_WhenZeroPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 0, "Zero points");

        // Assert
        // AwardPointsAsync returns early when points == 0, so no record should be created
        var gamification = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().BeNull();
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldHandleLargePoints()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 100000, "Large points");

        // Assert
        var gamification = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.TotalPoints.Should().Be(100000);
        // Level calculation: floor(sqrt(100000/100)) + 1 = floor(31.62) + 1 = 32
        // But it's capped at 100, so should be 100
        gamification.CurrentLevel.Should().BeLessThanOrEqualTo(100);
        gamification.CurrentTier.Should().Be("Diamond"); // 100000 points = Diamond
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateLevel_WhenPointsCrossThreshold()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 99, // Just below level 2
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
        await _service.AwardPointsAsync(userId, 1, "Level up");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentLevel.Should().Be(2);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateTier_WhenPointsCrossThreshold()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 500, // Bronze tier
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
        await _service.AwardPointsAsync(userId, 1, "Tier up");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentTier.Should().Be("Silver");
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldNotGoNegative()
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
        await _service.AwardPointsAsync(userId, -100, "Penalty");

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.TotalPoints.Should().Be(0); // Should not go negative
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCreateMultipleHistoryEntries()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 50, "First award");
        await _service.AwardPointsAsync(userId, 50, "Second award");
        await _service.AwardPointsAsync(userId, 50, "Third award");

        // Assert
        var history = await _context.PointsHistory
            .Where(h => h.UserId == userId)
            .ToListAsync();
        history.Should().HaveCount(3);
        history.Sum(h => h.PointsChange).Should().Be(150);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
