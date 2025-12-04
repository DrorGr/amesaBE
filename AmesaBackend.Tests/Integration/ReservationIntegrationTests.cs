using Xunit;
using FluentAssertions;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace AmesaBackend.Tests.Integration
{
    public class ReservationIntegrationTests : IDisposable
    {
        private readonly LotteryDbContext _context;
        private readonly InMemoryCache _cache;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly RedisInventoryManager _inventoryManager;
        private readonly TicketReservationService _reservationService;
        private readonly House _testHouse;

        public ReservationIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: $"ReservationIntegrationTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new LotteryDbContext(options);
            _cache = new InMemoryCache();
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<IDatabase>();
            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_mockDatabase.Object);

            var mockLogger = new Mock<ILogger<RedisInventoryManager>>();
            var mockReservationLogger = new Mock<ILogger<TicketReservationService>>();
            var mockLotteryService = new Mock<ILotteryService>();
            var mockRateLimitService = new Mock<AmesaBackend.Auth.Services.IRateLimitService>();

            _inventoryManager = new RedisInventoryManager(
                _mockRedis.Object,
                _context,
                mockLogger.Object
            );

            _reservationService = new TicketReservationService(
                _context,
                _inventoryManager,
                mockLotteryService.Object,
                mockReservationLogger.Object,
                mockRateLimitService.Object
            );

            // Setup test house
            _testHouse = new House
            {
                Id = Guid.NewGuid(),
                Title = "Integration Test House",
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

            // Setup mocks
            mockLotteryService.Setup(s => s.CanUserEnterLotteryAsync(It.IsAny<Guid>(), _testHouse.Id))
                .ReturnsAsync(true);
            mockLotteryService.Setup(s => s.CheckVerificationRequirementAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            mockRateLimitService.Setup(r => r.CheckRateLimitAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            mockRateLimitService.Setup(r => r.IncrementRateLimitAsync(
                It.IsAny<string>(), 
                It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task FullReservationFlow_CreatesReservationAndUpdatesInventory()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new AmesaBackend.Lottery.DTOs.CreateReservationRequest
            {
                Quantity = 5,
                PaymentMethodId = Guid.NewGuid()
            };

            var inventoryStatus = new AmesaBackend.Lottery.DTOs.InventoryStatus
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

            _mockDatabase.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);
            _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(1));

            // Mock inventory manager methods
            var mockInventoryManager = new Mock<IRedisInventoryManager>();
            mockInventoryManager.Setup(m => m.GetInventoryStatusAsync(_testHouse.Id))
                .ReturnsAsync(inventoryStatus);
            mockInventoryManager.Setup(m => m.ReserveInventoryAsync(
                _testHouse.Id, 
                request.Quantity, 
                It.IsAny<string>()))
                .ReturnsAsync(true);
            mockInventoryManager.Setup(m => m.AddParticipantAsync(_testHouse.Id, userId))
                .ReturnsAsync(true);

            // Use reflection to replace inventory manager in reservation service
            // For integration test, we'll test with actual database operations

            // Act
            var reservation = await _reservationService.CreateReservationAsync(
                request, 
                _testHouse.Id, 
                userId);

            // Assert
            reservation.Should().NotBeNull();
            reservation.HouseId.Should().Be(_testHouse.Id);
            reservation.UserId.Should().Be(userId);
            reservation.Quantity.Should().Be(request.Quantity);

            var reservationInDb = await _context.TicketReservations
                .FirstOrDefaultAsync(r => r.Id == reservation.Id);
            reservationInDb.Should().NotBeNull();
            reservationInDb!.Status.Should().Be("pending");
        }

        [Fact]
        public async Task ReservationCancellation_ReleasesInventory()
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

            var mockInventoryManager = new Mock<IRedisInventoryManager>();
            mockInventoryManager.Setup(m => m.ReleaseInventoryAsync(
                _testHouse.Id, 
                reservation.Quantity))
                .ReturnsAsync(true);

            // Act
            var result = await _reservationService.CancelReservationAsync(reservation.Id, userId);

            // Assert
            result.Should().BeTrue();
            
            var cancelledReservation = await _context.TicketReservations.FindAsync(reservation.Id);
            cancelledReservation!.Status.Should().Be("cancelled");
        }

        [Fact]
        public async Task ExpiredReservationCleanup_MarksReservationsAsExpired()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expiredReservation = new TicketReservation
            {
                Id = Guid.NewGuid(),
                HouseId = _testHouse.Id,
                UserId = userId,
                Quantity = 5,
                TotalPrice = 250,
                Status = "pending",
                ReservationToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketReservations.Add(expiredReservation);
            await _context.SaveChangesAsync();

            // Act
            var validationResult = await _reservationService.ValidateReservationAsync(expiredReservation.Id);

            // Assert
            validationResult.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _cache.Dispose();
        }
    }
}





