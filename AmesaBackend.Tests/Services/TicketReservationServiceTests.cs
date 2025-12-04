using Xunit;
using Moq;
using FluentAssertions;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Tests.Services
{
    public class TicketReservationServiceTests : IDisposable
    {
        private readonly LotteryDbContext _context;
        private readonly Mock<IRedisInventoryManager> _mockInventoryManager;
        private readonly Mock<ILotteryService> _mockLotteryService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<ILogger<TicketReservationService>> _mockLogger;
        private readonly TicketReservationService _reservationService;
        private readonly House _testHouse;

        public TicketReservationServiceTests()
        {
            var options = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new LotteryDbContext(options);
            _mockInventoryManager = new Mock<IRedisInventoryManager>();
            _mockLotteryService = new Mock<ILotteryService>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockLogger = new Mock<ILogger<TicketReservationService>>();

            _reservationService = new TicketReservationService(
                _context,
                _mockInventoryManager.Object,
                _mockLotteryService.Object,
                _mockLogger.Object,
                _mockRateLimitService.Object
            );

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
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateReservationAsync_WithValidRequest_CreatesReservation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateReservationRequest
            {
                Quantity = 5,
                PaymentMethodId = Guid.NewGuid()
            };

            var inventoryStatus = new InventoryStatus
            {
                HouseId = _testHouse.Id,
                TotalTickets = 1000,
                AvailableTickets = 500,
                ReservedTickets = 0,
                SoldTickets = 500,
                LotteryEndDate = _testHouse.LotteryEndDate,
                TimeRemaining = TimeSpan.FromDays(7),
                IsSoldOut = false,
                IsEnded = false
            };

            _mockLotteryService.Setup(s => s.CanUserEnterLotteryAsync(userId, _testHouse.Id))
                .ReturnsAsync(true);
            _mockLotteryService.Setup(s => s.CheckVerificationRequirementAsync(userId))
                .Returns(Task.CompletedTask);
            _mockInventoryManager.Setup(m => m.GetInventoryStatusAsync(_testHouse.Id))
                .ReturnsAsync(inventoryStatus);
            _mockInventoryManager.Setup(m => m.ReserveInventoryAsync(
                _testHouse.Id, 
                request.Quantity, 
                It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockInventoryManager.Setup(m => m.AddParticipantAsync(_testHouse.Id, userId))
                .ReturnsAsync(true);
            _mockRateLimitService.Setup(r => r.CheckRateLimitAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockRateLimitService.Setup(r => r.IncrementRateLimitAsync(
                It.IsAny<string>(), 
                It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _reservationService.CreateReservationAsync(request, _testHouse.Id, userId);

            // Assert
            result.Should().NotBeNull();
            result.HouseId.Should().Be(_testHouse.Id);
            result.UserId.Should().Be(userId);
            result.Quantity.Should().Be(request.Quantity);
            result.Status.Should().Be("pending");
            result.TotalPrice.Should().Be(_testHouse.TicketPrice * request.Quantity);

            var reservationInDb = await _context.TicketReservations.FindAsync(result.Id);
            reservationInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateReservationAsync_WithInsufficientTickets_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateReservationRequest { Quantity = 1000 };

            var inventoryStatus = new InventoryStatus
            {
                HouseId = _testHouse.Id,
                AvailableTickets = 10,
                IsSoldOut = false,
                IsEnded = false,
                LotteryEndDate = _testHouse.LotteryEndDate,
                TimeRemaining = TimeSpan.FromDays(7)
            };

            _mockInventoryManager.Setup(m => m.GetInventoryStatusAsync(_testHouse.Id))
                .ReturnsAsync(inventoryStatus);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _reservationService.CreateReservationAsync(request, _testHouse.Id, userId));
        }

        [Fact]
        public async Task CancelReservationAsync_WithPendingReservation_CancelsReservation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var reservation = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = userId,
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.Add(reservation);
            await _context.SaveChangesAsync();

            _mockInventoryManager.Setup(m => m.ReleaseInventoryAsync(
                _testHouse.Id, 
                reservation.Quantity))
                .ReturnsAsync(true);

            // Act
            var result = await _reservationService.CancelReservationAsync(reservation.Id, userId);

            // Assert
            result.Should().BeTrue();

            var updatedReservation = await _context.TicketReservations.FindAsync(reservation.Id);
            updatedReservation!.Status.Should().Be("cancelled");
        }

        [Fact]
        public async Task ValidateReservationAsync_WithValidReservation_ReturnsTrue()
        {
            // Arrange
            var reservation = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = Guid.NewGuid(),
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reservationService.ValidateReservationAsync(reservation.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateReservationAsync_WithExpiredReservation_ReturnsFalse()
        {
            // Arrange
            var reservation = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = Guid.NewGuid(),
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reservationService.ValidateReservationAsync(reservation.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserReservationsAsync_WithExistingReservations_ReturnsReservations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var reservation1 = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = userId,
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var reservation2 = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = userId,
                Quantity = 3,
                TotalPrice = 150,
                Status = "completed",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.AddRange(reservation1, reservation2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reservationService.GetUserReservationsAsync(userId);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUserReservationsAsync_WithStatusFilter_ReturnsFilteredReservations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var reservation = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = userId,
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reservationService.GetUserReservationsAsync(userId, "pending");

            // Assert
            result.Should().HaveCount(1);
            result[0].Status.Should().Be("pending");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}





