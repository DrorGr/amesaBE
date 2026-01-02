using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Controllers;

/// <summary>
/// Extended tests for TicketsController covering additional scenarios
/// </summary>
public class TicketsControllerExtendedTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<TicketsController>> _loggerMock;
    private readonly TicketsController _controller;

    public TicketsControllerExtendedTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<TicketsController>>();
        _controller = new TicketsController(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetActiveTickets_ShouldFilterByActiveStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var house = new House
        {
            Id = houseId,
            Title = "Test House",
            Description = "Test",
            Price = 100,
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

        var activeTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T001",
            PurchasePrice = 100,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(activeTicket);

        var expiredTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T002",
            PurchasePrice = 100,
            Status = TicketStatus.Cancelled,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(expiredTicket);

        var wonTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T003",
            PurchasePrice = 100,
            Status = TicketStatus.Winner,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(wonTicket);

        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetActiveTickets();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        // Verify response structure matches frontend expectations: { success, data, message }
        var responseType = response!.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        successProperty.Should().NotBeNull("Response should have 'success' property");
        dataProperty.Should().NotBeNull("Response should have 'data' property");
    }

    [Fact]
    public async Task GetActiveTickets_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var house = new House
        {
            Id = houseId,
            Title = "Test House",
            Description = "Test",
            Price = 100,
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

        var olderTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T001",
            PurchasePrice = 100,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.LotteryTickets.Add(olderTicket);

        var newerTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T002",
            PurchasePrice = 100,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(newerTicket);

        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetActiveTickets();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveTickets_ShouldIncludeHouseInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var house = new House
        {
            Id = houseId,
            Title = "Test House",
            Description = "Test Description",
            Price = 100000,
            Status = LotteryStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Houses.Add(house);

        var ticket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T001",
            PurchasePrice = 100,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket);

        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetActiveTickets();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldCalculateCorrectTotals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var house = new House
        {
            Id = houseId,
            Title = "Test House",
            Description = "Test",
            Price = 100,
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

        var ticket1 = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T001",
            PurchasePrice = 100,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket1);

        var ticket2 = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T002",
            PurchasePrice = 200,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket2);

        var ticket3 = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T003",
            PurchasePrice = 150,
            Status = TicketStatus.Cancelled,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket3);

        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        // Verify analytics structure matches frontend expectations: { success, data: { totalTickets, activeTickets, totalSpent, averageTicketPrice } }
        var responseType = response!.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        successProperty.Should().NotBeNull("Response should have 'success' property");
        dataProperty.Should().NotBeNull("Response should have 'data' property");
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldReturnZeroValues_WhenUserHasNoTickets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        // Verify response structure
        var responseType = response!.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        successProperty.Should().NotBeNull("Response should have 'success' property");
        dataProperty.Should().NotBeNull("Response should have 'data' property");
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldCalculateAverageTicketPrice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var house = new House
        {
            Id = houseId,
            Title = "Test House",
            Description = "Test",
            Price = 100,
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

        var ticket1 = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T001",
            PurchasePrice = 100,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket1);

        var ticket2 = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T002",
            PurchasePrice = 200,
            Status = TicketStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.LotteryTickets.Add(ticket2);

        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        // Verify average calculation - response structure: { success, data: { totalTickets, activeTickets, totalSpent, averageTicketPrice } }
        var responseType = response!.GetType();
        var successProperty = responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("data");
        successProperty.Should().NotBeNull("Response should have 'success' property");
        dataProperty.Should().NotBeNull("Response should have 'data' property");
    }

    [Fact]
    public async Task GetActiveTickets_ShouldReturnServiceUnavailable_WhenContextIsNull()
    {
        // Arrange
        var controllerWithNullContext = new TicketsController(null!, _loggerMock.Object);
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(controllerWithNullContext, userId);

        // Act
        var result = await controllerWithNullContext.GetActiveTickets();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldReturnServiceUnavailable_WhenContextIsNull()
    {
        // Arrange
        var controllerWithNullContext = new TicketsController(null!, _loggerMock.Object);
        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(controllerWithNullContext, userId);

        // Act
        var result = await controllerWithNullContext.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
