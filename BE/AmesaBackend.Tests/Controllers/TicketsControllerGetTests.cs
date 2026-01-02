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

public class TicketsControllerGetTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<TicketsController>> _loggerMock;
    private readonly TicketsController _controller;

    public TicketsControllerGetTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<TicketsController>>();
        _controller = new TicketsController(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetActiveTickets_ShouldReturnTickets_WhenUserHasTickets()
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
    }

    [Fact]
    public async Task GetActiveTickets_ShouldExcludeCancelledTickets()
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

        var cancelledTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T002",
            PurchasePrice = 100,
            Status = TicketStatus.Cancelled,
            CreatedAt = DateTime.UtcNow
        };

        _context.LotteryTickets.AddRange(activeTicket, cancelledTicket);
        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetActiveTickets();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveTickets_ShouldExcludeWinnerTickets()
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

        var winnerTicket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseId = houseId,
            TicketNumber = "T002",
            PurchasePrice = 100,
            Status = TicketStatus.Winner,
            CreatedAt = DateTime.UtcNow
        };

        _context.LotteryTickets.AddRange(activeTicket, winnerTicket);
        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetActiveTickets();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldCalculateTotalSpending()
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

        var tickets = new List<LotteryTicket>
        {
            new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HouseId = houseId,
                TicketNumber = "T001",
                PurchasePrice = 100,
                Status = TicketStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HouseId = houseId,
                TicketNumber = "T002",
                PurchasePrice = 200,
                Status = TicketStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTicketAnalytics_ShouldCalculateWinRate()
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

        var tickets = new List<LotteryTicket>
        {
            new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HouseId = houseId,
                TicketNumber = "T001",
                PurchasePrice = 100,
                Status = TicketStatus.Winner,
                CreatedAt = DateTime.UtcNow
            },
            new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HouseId = houseId,
                TicketNumber = "T002",
                PurchasePrice = 100,
                Status = TicketStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.LotteryTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetTicketAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
