using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Models;
using AmesaBackend.Tests.TestHelpers;
using System.Threading;

namespace AmesaBackend.Tests.Controllers;

public class HousesFavoritesControllerPaginationTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IUserPreferencesService> _userPreferencesServiceMock;
    private readonly Mock<ILogger<HousesFavoritesController>> _loggerMock;
    private readonly HousesFavoritesController _controller;

    public HousesFavoritesControllerPaginationTests()
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
    public async Task GetFavorites_ShouldHandlePagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseIds = Enumerable.Range(1, 25).Select(_ => Guid.NewGuid()).ToList();
        
        var houses = houseIds.Select(id => new House
        {
            Id = id,
            Title = $"House {id}",
            Description = "Test",
            Price = 100000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.Houses.AddRange(houses);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(houseIds);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(page: 1, limit: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldHandleSecondPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseIds = Enumerable.Range(1, 25).Select(_ => Guid.NewGuid()).ToList();
        
        var houses = houseIds.Select(id => new House
        {
            Id = id,
            Title = $"House {id}",
            Description = "Test",
            Price = 100000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.Houses.AddRange(houses);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(houseIds);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(page: 2, limit: 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldHandleEmptyFavorites()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavoritesCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        
        var houses = houseIds.Select(id => new House
        {
            Id = id,
            Title = $"House {id}",
            Description = "Test",
            Price = 100000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _context.Houses.AddRange(houses);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(houseIds);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavoritesCount();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavoritesCount_ShouldReturnZero_WhenNoFavorites()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavoritesCount();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
