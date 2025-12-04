using Xunit;
using FluentAssertions;
using AmesaBackend.Lottery.Services.Processors;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AmesaBackend.Tests.Services
{
    public class TicketCreatorProcessorTests : IDisposable
    {
        private readonly LotteryDbContext _context;
        private readonly Mock<ILogger<TicketCreatorProcessor>> _mockLogger;
        private readonly TicketCreatorProcessor _processor;
        private readonly House _testHouse;

        public TicketCreatorProcessorTests()
        {
            var options = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new LotteryDbContext(options);
            _mockLogger = new Mock<ILogger<TicketCreatorProcessor>>();

            _processor = new TicketCreatorProcessor(_context, _mockLogger.Object);

            // Setup test house
            _testHouse = new House
            {
                Id = Guid.NewGuid(),
                Title = "Test House",
                Price = 500000,
                TotalTickets = 1000,
                TicketPrice = 50,
                Status = "Active",
                LotteryEndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Houses.Add(_testHouse);

            // Setup test reservation
            var reservation = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = Guid.NewGuid(),
                Quantity = 5,
                TotalPrice = 250,
                Status = "processing",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.Add(reservation);
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateTicketsAsync_WithValidReservation_CreatesTickets()
        {
            // Arrange
            var reservation = _context.TicketReservations.First();
            var transactionId = Guid.NewGuid();

            // Act
            var result = await _processor.CreateTicketsAsync(reservation.Id, transactionId);

            // Assert
            result.Success.Should().BeTrue();
            result.TicketIds.Should().HaveCount(reservation.Quantity);

            var tickets = await _context.LotteryTickets
                .Where(t => t.HouseId == _testHouse.Id)
                .ToListAsync();
            tickets.Should().HaveCount(reservation.Quantity);

            var updatedReservation = await _context.TicketReservations.FindAsync(reservation.Id);
            updatedReservation!.Status.Should().Be("completed");
            updatedReservation.PaymentTransactionId.Should().Be(transactionId);
        }

        [Fact]
        public async Task CreateTicketsAsync_WithInvalidReservation_ReturnsFailure()
        {
            // Arrange
            var invalidReservationId = Guid.NewGuid();
            var transactionId = Guid.NewGuid();

            // Act
            var result = await _processor.CreateTicketsAsync(invalidReservationId, transactionId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}





