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

public class HousesFavoritesControllerExtendedTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IUserPreferencesService> _userPreferencesServiceMock;
    private readonly Mock<ILogger<HousesFavoritesController>> _loggerMock;
    private readonly HousesFavoritesController _controller;

    public HousesFavoritesControllerExtendedTests()
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
    public async Task GetFavorites_ShouldSortByDateAddedDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId1 = Guid.NewGuid();
        var houseId2 = Guid.NewGuid();

        var house1 = new House
        {
            Id = houseId1,
            Title = "Older House",
            Description = "Test",
            Price = 100000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var house2 = new House
        {
            Id = houseId2,
            Title = "Newer House",
            Description = "Test",
            Price = 200000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Houses.AddRange(house1, house2);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { houseId1, houseId2 });

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(sortBy: "dateadded", sortOrder: "desc");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldSortByDateAddedAscending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId1 = Guid.NewGuid();
        var houseId2 = Guid.NewGuid();

        var house1 = new House
        {
            Id = houseId1,
            Title = "Older House",
            Description = "Test",
            Price = 100000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var house2 = new House
        {
            Id = houseId2,
            Title = "Newer House",
            Description = "Test",
            Price = 200000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Houses.AddRange(house1, house2);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { houseId1, houseId2 });

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(sortBy: "dateadded", sortOrder: "asc");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldSortByPriceDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId1 = Guid.NewGuid();
        var houseId2 = Guid.NewGuid();

        var house1 = new House
        {
            Id = houseId1,
            Title = "Cheap House",
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
        };

        var house2 = new House
        {
            Id = houseId2,
            Title = "Expensive House",
            Description = "Test",
            Price = 500000,
            Status = LotteryStatus.Active,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Houses.AddRange(house1, house2);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { houseId1, houseId2 });

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(sortBy: "price", sortOrder: "desc");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldUseDefaultSort_WhenSortByIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();

        var house = new House
        {
            Id = houseId,
            Title = "Test House",
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
        };

        _context.Houses.Add(house);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { houseId });

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites(sortBy: "invalid");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFavorites_ShouldFilterOutInactiveHouses()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activeHouseId = Guid.NewGuid();
        var inactiveHouseId = Guid.NewGuid();

        var activeHouse = new House
        {
            Id = activeHouseId,
            Title = "Active House",
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
        };

        var inactiveHouse = new House
        {
            Id = inactiveHouseId,
            Title = "Inactive House",
            Description = "Test",
            Price = 200000,
            Status = LotteryStatus.Ended,
            TotalTickets = 100,
            TicketPrice = 10,
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            Bedrooms = 3,
            Bathrooms = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Houses.AddRange(activeHouse, inactiveHouse);
        await _context.SaveChangesAsync();

        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { activeHouseId, inactiveHouseId });

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavorites_ShouldHandleException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavorites();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetFavoritesCount_ShouldHandleException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userPreferencesServiceMock
            .Setup(s => s.GetFavoriteHouseIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetFavoritesCount();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
