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

public class GamificationServiceTierTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceTierTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnBronze_For0Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(0);

        // Assert
        result.Should().Be("Bronze");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnBronze_For500Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(500);

        // Assert
        result.Should().Be("Bronze");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnSilver_For501Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(501);

        // Assert
        result.Should().Be("Silver");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnSilver_For2000Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(2000);

        // Assert
        result.Should().Be("Silver");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnGold_For2001Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(2001);

        // Assert
        result.Should().Be("Gold");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnGold_For5000Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(5000);

        // Assert
        result.Should().Be("Gold");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnPlatinum_For5001Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(5001);

        // Assert
        result.Should().Be("Platinum");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnPlatinum_For10000Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(10000);

        // Assert
        result.Should().Be("Platinum");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnDiamond_For10001Points()
    {
        // Act
        var result = await _service.CalculateTierAsync(10001);

        // Assert
        result.Should().Be("Diamond");
    }

    [Fact]
    public async Task CalculateTierAsync_ShouldReturnDiamond_ForLargePoints()
    {
        // Act
        var result = await _service.CalculateTierAsync(100000);

        // Assert
        result.Should().Be("Diamond");
    }

    [Fact]
    public async Task AwardPointsAsync_ShouldUpdateTier_WhenPointsCrossThreshold()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.AwardPointsAsync(userId, 500, "Test"); // Bronze

        // Act - Award enough to cross to Silver
        await _service.AwardPointsAsync(userId, 1, "Test");

        // Assert
        var gamification = await _context.UserGamification
            .FirstOrDefaultAsync(g => g.UserId == userId);
        gamification.Should().NotBeNull();
        gamification!.CurrentTier.Should().Be("Silver");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
