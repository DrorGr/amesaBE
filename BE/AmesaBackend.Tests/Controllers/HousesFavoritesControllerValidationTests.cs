using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Tests.TestHelpers;
using System.Threading;

namespace AmesaBackend.Tests.Controllers;

public class HousesFavoritesControllerValidationTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IUserPreferencesService> _userPreferencesServiceMock;
    private readonly Mock<ILogger<HousesFavoritesController>> _loggerMock;
    private readonly HousesFavoritesController _controller;

    public HousesFavoritesControllerValidationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _userPreferencesServiceMock = new Mock<IUserPreferencesService>();
        _loggerMock = new Mock<ILogger<HousesFavoritesController>>();
        _controller = new HousesFavoritesController(
            _userPreferencesServiceMock.Object,
            _context,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnBadRequest_WhenPageIsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(page: 0);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnBadRequest_WhenPageIsNegative()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(page: -1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnBadRequest_WhenLimitExceedsMax()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(limit: 101); // Max is 100

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnBadRequest_WhenLimitIsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(limit: 0);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnBadRequest_WhenLimitIsNegative()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(limit: -1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldHandleNullFavoriteHouseIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Guid>?)null);

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldHandleEmptyFavoriteHouseIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
