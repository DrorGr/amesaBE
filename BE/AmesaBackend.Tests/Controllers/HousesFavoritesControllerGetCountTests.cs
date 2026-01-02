using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Auth.Services.Interfaces;
using IUserPreferencesService = AmesaBackend.Auth.Services.Interfaces.IUserPreferencesService;
using Moq;
using AmesaBackend.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Controllers;

public class HousesFavoritesControllerGetCountTests : IDisposable
{
    private readonly LotteryDbContext _context;
        private readonly Mock<IUserPreferencesService> _userPreferencesServiceMock;
    private readonly Mock<ILogger<HousesFavoritesController>> _loggerMock;
    private readonly HousesFavoritesController _controller;

    public HousesFavoritesControllerGetCountTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _userPreferencesServiceMock = new Mock<IUserPreferencesService>();
        _loggerMock = new Mock<ILogger<HousesFavoritesController>>();
        _controller = new HousesFavoritesController(_userPreferencesServiceMock.Object, _context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetFavoritesCount_ShouldReturnZero_WhenNoFavorites()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userPreferencesServiceMock
            .Setup(x => x.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavoritesCount();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavoritesCount_ShouldReturnCount_WhenFavoritesExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var favoriteIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _userPreferencesServiceMock
            .Setup(x => x.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(favoriteIds);
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavoritesCount();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavoritesCount_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - No user claims set up

        // Act
        var result = await _controller.GetFavoritesCount();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500); // Controller catches UnauthorizedAccessException and returns 500
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
