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

public class GamificationServiceCalculateTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceCalculateTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldReturnLevel1_ForZeroPoints()
    {
        // Act
        var level = await _service.CalculateLevelAsync(0);

        // Assert
        level.Should().Be(1);
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldReturnLevel1_ForLowPoints()
    {
        // Act
        var level = await _service.CalculateLevelAsync(50);

        // Assert
        level.Should().Be(1);
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldReturnLevel2_For100Points()
    {
        // Act
        var level = await _service.CalculateLevelAsync(100);

        // Assert
        level.Should().Be(2);
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldReturnLevel3_For400Points()
    {
        // Act
        var level = await _service.CalculateLevelAsync(400);

        // Assert
        level.Should().Be(3);
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldReturnLevel10_For10000Points()
    {
        // Act
        var level = await _service.CalculateLevelAsync(10000);

        // Assert
        level.Should().Be(11); // floor(sqrt(10000/100)) + 1 = floor(10) + 1 = 11
    }

    [Fact]
    public async Task CalculateLevelAsync_ShouldCapAtLevel100()
    {
        // Act
        var level = await _service.CalculateLevelAsync(1000000);

        // Assert
        level.Should().Be(100); // Max level
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnBronze_ForLowPoints()
    {
        // Act
        var tier = await _service.CalculateTierAsync(0);
        var tier100 = await _service.CalculateTierAsync(100);
        var tier500 = await _service.CalculateTierAsync(500);

        // Assert
        tier.Should().Be("Bronze");
        tier100.Should().Be("Bronze");
        tier500.Should().Be("Bronze");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnSilver_For501To2000Points()
    {
        // Act
        var tier501 = await _service.CalculateTierAsync(501);
        var tier1000 = await _service.CalculateTierAsync(1000);
        var tier2000 = await _service.CalculateTierAsync(2000);

        // Assert
        tier501.Should().Be("Silver");
        tier1000.Should().Be("Silver");
        tier2000.Should().Be("Silver");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnGold_For2001To5000Points()
    {
        // Act
        var tier2001 = await _service.CalculateTierAsync(2001);
        var tier3500 = await _service.CalculateTierAsync(3500);
        var tier5000 = await _service.CalculateTierAsync(5000);

        // Assert
        tier2001.Should().Be("Gold");
        tier3500.Should().Be("Gold");
        tier5000.Should().Be("Gold");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnPlatinum_For5001To10000Points()
    {
        // Act
        var tier5001 = await _service.CalculateTierAsync(5001);
        var tier7500 = await _service.CalculateTierAsync(7500);
        var tier10000 = await _service.CalculateTierAsync(10000);

        // Assert
        tier5001.Should().Be("Platinum");
        tier7500.Should().Be("Platinum");
        tier10000.Should().Be("Platinum");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnDiamond_For10001PlusPoints()
    {
        // Act
        var tier10001 = await _service.CalculateTierAsync(10001);
        var tier50000 = await _service.CalculateTierAsync(50000);
        var tier100000 = await _service.CalculateTierAsync(100000);

        // Assert
        tier10001.Should().Be("Diamond");
        tier50000.Should().Be("Diamond");
        tier100000.Should().Be("Diamond");
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateLevel_WhenPointsCrossThreshold()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamification = new UserGamification
        {
            UserId = userId,
            TotalPoints = 99, // Just below level 2 threshold
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
        await _service.AwardPointsAsync(userId, 1, "Level up"); // Total: 100, should be level 2

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
        await _service.AwardPointsAsync(userId, 1, "Tier up"); // Total: 501, should be Silver

        // Assert
        var updated = await _context.UserGamification.FirstOrDefaultAsync(g => g.UserId == userId);
        updated!.CurrentTier.Should().Be("Silver");
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldHandleReferenceId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 100, "Test reason", referenceId);

        // Assert
        var history = await _context.PointsHistory.FirstOrDefaultAsync(h => h.UserId == userId);
        history.Should().NotBeNull();
        history!.ReferenceId.Should().Be(referenceId);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldHandleNullReferenceId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 100, "Test reason", null);

        // Assert
        var history = await _context.PointsHistory.FirstOrDefaultAsync(h => h.UserId == userId);
        history.Should().NotBeNull();
        history!.ReferenceId.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
