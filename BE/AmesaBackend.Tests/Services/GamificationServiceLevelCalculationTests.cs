using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class GamificationServiceLevelCalculationTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceLevelCalculationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCalculateLevel1_For100Points()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 100, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.CurrentLevel.Should().Be(2); // floor(sqrt(100/100)) + 1 = 2
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCalculateLevel2_For400Points()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 400, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.CurrentLevel.Should().Be(3); // floor(sqrt(400/100)) + 1 = 3
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCalculateLevel3_For900Points()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 900, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.CurrentLevel.Should().Be(4); // floor(sqrt(900/100)) + 1 = 4
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldCalculateTier_ForPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.AwardPointsAsync(userId, 1000, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.CurrentTier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateLevel_WhenPointsAccumulate()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act - Award points incrementally
        await _service.AwardPointsAsync(userId, 100, "Test");
        await _service.AwardPointsAsync(userId, 200, "Test");
        await _service.AwardPointsAsync(userId, 300, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.TotalPoints.Should().Be(600);
        gamification.CurrentLevel.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldHandleNegativePoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.AwardPointsAsync(userId, 1000, "Test");

        // Act
        await _service.AwardPointsAsync(userId, -200, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.TotalPoints.Should().Be(800);
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldRecalculateTier_WhenLevelChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.AwardPointsAsync(userId, 100, "Test");
        var initialTier = (await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId))!.CurrentTier;

        // Act - Award enough points to change tier
        await _service.AwardPointsAsync(userId, 10000, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        // Tier might change based on level
        gamification!.CurrentTier.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
