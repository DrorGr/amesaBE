using Xunit;
using Moq;
using FluentAssertions;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Tests.Services
{
    public class RedisInventoryManagerTests
    {
        private readonly LotteryDbContext _context;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<ILogger<RedisInventoryManager>> _mockLogger;
        private readonly RedisInventoryManager _inventoryManager;

        public RedisInventoryManagerTests()
        {
            var options = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new LotteryDbContext(options);
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<IDatabase>();
            _mockLogger = new Mock<ILogger<RedisInventoryManager>>();

            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_mockDatabase.Object);

            _inventoryManager = new RedisInventoryManager(
                _mockRedis.Object,
                _context,
                _mockLogger.Object
            );

            // Setup test house
            var house = new House
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
            _context.Houses.Add(house);
            _context.SaveChanges();
        }

        [Fact]
        public async Task ReserveInventoryAsync_WithAvailableTickets_ReturnsTrue()
        {
            // Arrange
            var houseId = _context.Houses.First().Id;
            var quantity = 5;
            var token = Guid.NewGuid().ToString();

            _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(1)); // Success

            // Act
            var result = await _inventoryManager.ReserveInventoryAsync(houseId, quantity, token);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ReserveInventoryAsync_WithInsufficientTickets_ReturnsFalse()
        {
            // Arrange
            var houseId = _context.Houses.First().Id;
            var quantity = 2000; // More than total
            var token = Guid.NewGuid().ToString();

            _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(0)); // Failure

            // Act
            var result = await _inventoryManager.ReserveInventoryAsync(houseId, quantity, token);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ReleaseInventoryAsync_WithValidQuantity_ReturnsTrue()
        {
            // Arrange
            var houseId = _context.Houses.First().Id;
            var quantity = 5;

            _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(1));

            // Act
            var result = await _inventoryManager.ReleaseInventoryAsync(houseId, quantity);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetInventoryStatusAsync_WithValidHouse_ReturnsStatus()
        {
            // Arrange
            var house = _context.Houses.First();
            var houseId = house.Id;

            // Mock Redis returns
            _mockDatabase.Setup(db => db.StringGetAsync(
                It.Is<string>(k => k.Contains("inventory") && !k.Contains("reserved") && !k.Contains("sold")),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null); // Fallback to database

            // Act
            var result = await _inventoryManager.GetInventoryStatusAsync(houseId);

            // Assert
            result.Should().NotBeNull();
            result.HouseId.Should().Be(houseId);
            result.TotalTickets.Should().Be(house.TotalTickets);
            result.AvailableTickets.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetAvailableCountAsync_WithValidHouse_ReturnsCount()
        {
            // Arrange
            var houseId = _context.Houses.First().Id;

            _mockDatabase.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null); // Fallback to database

            // Act
            var result = await _inventoryManager.GetAvailableCountAsync(houseId);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task CheckParticipantCapAsync_WithNoCap_ReturnsTrue()
        {
            // Arrange
            var houseId = _context.Houses.First().Id;
            var userId = Guid.NewGuid();
            var house = _context.Houses.First();
            house.MaxParticipants = null;
            _context.SaveChanges();

            _mockDatabase.Setup(db => db.SetContainsAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            _mockDatabase.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            // Act
            var result = await _inventoryManager.CheckParticipantCapAsync(houseId, userId);

            // Assert
            result.Should().BeTrue(); // No cap means always true
        }

        [Fact]
        public async Task AddParticipantAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var houseId = _context.Houses.First().Id;
            var userId = Guid.NewGuid();

            _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(1));

            // Act
            var result = await _inventoryManager.AddParticipantAsync(houseId, userId);

            // Assert
            result.Should().BeTrue();
        }

        private void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}





