extern alias AuthApp;
using Xunit;
using Moq;
using FluentAssertions;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Hubs;
using AuthApp::AmesaBackend.Auth.Services;
using AuthApp::AmesaBackend.Auth.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Events;
using AmesaBackend.Tests.TestHelpers;
using System.Threading;

namespace AmesaBackend.Tests.Services
{
    public class LotteryServiceFavoritesTests : IDisposable
    {
        private readonly LotteryDbContext _context;
        private readonly AuthDbContext _authContext;
        private readonly Mock<IUserPreferencesService> _mockUserPreferencesService;
        private readonly Mock<IHubContext<LotteryHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IGroupManager> _mockGroupManager;
        private readonly Mock<ICache> _mockCache;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly Mock<ILogger<LotteryService>> _mockLogger;
        private readonly LotteryService _lotteryService;
        private readonly House _testHouse;
        private readonly Guid _testUserId;

        public LotteryServiceFavoritesTests()
        {
            var lotteryOptions = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: $"LotteryTestDb_{Guid.NewGuid()}")
                .Options;

            var authOptions = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(databaseName: $"AuthTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new LotteryDbContext(lotteryOptions);
            _authContext = new AuthDbContext(authOptions);
            _mockUserPreferencesService = new Mock<IUserPreferencesService>();
            _mockHubContext = new Mock<IHubContext<LotteryHub>>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockGroupManager = new Mock<IGroupManager>();
            _mockCache = new Mock<ICache>();
            _mockEventPublisher = new Mock<IEventPublisher>();
            _mockLogger = new Mock<ILogger<LotteryService>>();

            // Setup SignalR mocks
            _mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClientProxy.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Setup test house
            _testHouse = new House
            {
                Id = Guid.NewGuid(),
                Title = "Test House",
                Price = 500000,
                Location = "Test Location",
                TotalTickets = 1000,
                TicketPrice = 50,
                Status = "Active",
                LotteryEndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeletedAt = null
            };
            _context.Houses.Add(_testHouse);
            _context.SaveChanges();

            _testUserId = Guid.NewGuid();

            _lotteryService = new LotteryService(
                _context,
                _mockEventPublisher.Object,
                _mockLogger.Object,
                _mockUserPreferencesService.Object,
                null, // configurationService
                _authContext,
                null, // httpRequest
                null, // configuration
                null, // redis
                _mockHubContext.Object,
                _mockCache.Object,
                null // gamificationService
            );
        }

        [Fact]
        public async Task AddHouseToFavoritesAsync_WithValidHouse_ReturnsTrue()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            result.Should().BeTrue();
            _mockUserPreferencesService.Verify(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task AddHouseToFavoritesAsync_WithNonExistentHouse_ReturnsFalse()
        {
            // Arrange
            var nonExistentHouseId = Guid.NewGuid();

            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, nonExistentHouseId);

            // Assert
            result.Should().BeFalse();
            _mockUserPreferencesService.Verify(s => s.AddHouseToFavoritesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task AddHouseToFavoritesAsync_WithSoftDeletedHouse_ReturnsFalse()
        {
            // Arrange
            var deletedHouse = new House
            {
                Id = Guid.NewGuid(),
                Title = "Deleted House",
                DeletedAt = DateTime.UtcNow
            };
            _context.Houses.Add(deletedHouse);
            await _context.SaveChangesAsync();

            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, deletedHouse.Id);

            // Assert
            result.Should().BeFalse();
            _mockUserPreferencesService.Verify(s => s.AddHouseToFavoritesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task AddHouseToFavoritesAsync_OnSuccess_BroadcastsSignalRUpdate()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            _mockClientProxy.Verify(c => c.SendAsync(
                "FavoriteUpdate",
                It.Is<FavoriteUpdateDto>(dto => 
                    dto.HouseId == _testHouse.Id &&
                    dto.UpdateType == "added" &&
                    dto.HouseTitle == _testHouse.Title),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task AddHouseToFavoritesAsync_OnSuccess_InvalidatesCache()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            _mockCache.Verify(c => c.RemoveRecordAsync($"lottery:favorites:{_testUserId}", It.IsAny<bool>()), Times.Once);
            _mockCache.Verify(c => c.RemoveRecordAsync($"lottery:favorites:houses:{_testUserId}", It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RemoveHouseFromFavoritesAsync_WithValidHouse_ReturnsTrue()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            result.Should().BeTrue();
            _mockUserPreferencesService.Verify(s => s.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveHouseFromFavoritesAsync_WithNonExistentHouse_ReturnsFalse()
        {
            // Arrange
            var nonExistentHouseId = Guid.NewGuid();

            // Act
            var result = await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, nonExistentHouseId);

            // Assert
            result.Should().BeFalse();
            _mockUserPreferencesService.Verify(s => s.RemoveHouseFromFavoritesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RemoveHouseFromFavoritesAsync_OnSuccess_BroadcastsSignalRUpdate()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            _mockClientProxy.Verify(c => c.SendAsync(
                "FavoriteUpdate",
                It.Is<object[]>(args => args.Length > 0 && args[0] is FavoriteUpdateDto dto && 
                    dto.HouseId == _testHouse.Id &&
                    dto.UpdateType == "removed" &&
                    dto.HouseTitle == _testHouse.Title),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetUserFavoriteHousesAsync_WithCachedIds_ReturnsHousesFromCache()
        {
            // Arrange
            var favoriteIds = new List<Guid> { _testHouse.Id };
            _mockCache
                .Setup(c => c.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}", It.IsAny<bool>()))
                .ReturnsAsync(favoriteIds);
            _mockUserPreferencesService
                .Setup(s => s.GetFavoriteHouseIdsAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favoriteIds);

            // Act
            var result = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(_testHouse.Id);
            _mockCache.Verify(c => c.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}", It.IsAny<bool>()), Times.Once);
            _mockUserPreferencesService.Verify(s => s.GetFavoriteHouseIdsAsync(_testUserId, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetUserFavoriteHousesAsync_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var house1 = new House { Id = Guid.NewGuid(), Title = "House 1", Price = 100000, Location = "Location 1", Status = "Active", LotteryEndDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow };
            var house2 = new House { Id = Guid.NewGuid(), Title = "House 2", Price = 200000, Location = "Location 2", Status = "Active", LotteryEndDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow };
            var house3 = new House { Id = Guid.NewGuid(), Title = "House 3", Price = 300000, Location = "Location 3", Status = "Active", LotteryEndDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow };
            _context.Houses.AddRange(house1, house2, house3);
            await _context.SaveChangesAsync();

            var favoriteIds = new List<Guid> { house1.Id, house2.Id, house3.Id };
            _mockUserPreferencesService
                .Setup(s => s.GetFavoriteHouseIdsAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favoriteIds);

            // Act
            var result = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId, page: 1, limit: 2);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUserFavoriteHousesAsync_WithSorting_ReturnsSortedResults()
        {
            // Arrange
            var house1 = new House { Id = Guid.NewGuid(), Title = "House A", Price = 300000, Location = "Location 1", Status = "Active", LotteryEndDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow };
            var house2 = new House { Id = Guid.NewGuid(), Title = "House B", Price = 100000, Location = "Location 2", Status = "Active", LotteryEndDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow };
            var house3 = new House { Id = Guid.NewGuid(), Title = "House C", Price = 200000, Location = "Location 3", Status = "Active", LotteryEndDate = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow };
            _context.Houses.AddRange(house1, house2, house3);
            await _context.SaveChangesAsync();

            var favoriteIds = new List<Guid> { house1.Id, house2.Id, house3.Id };
            _mockUserPreferencesService
                .Setup(s => s.GetFavoriteHouseIdsAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favoriteIds);

            // Act
            var result = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId, sortBy: "price", sortOrder: "asc");

            // Assert
            result.Should().HaveCount(3);
            result[0].Price.Should().Be(100000); // house2
            result[1].Price.Should().Be(200000); // house3
            result[2].Price.Should().Be(300000); // house1
        }

        [Fact]
        public async Task GetUserFavoriteHousesCountAsync_WithCachedIds_ReturnsCountFromCache()
        {
            // Arrange
            var favoriteIds = new List<Guid> { _testHouse.Id, Guid.NewGuid() };
            _mockCache
                .Setup(c => c.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}", It.IsAny<bool>()))
                .ReturnsAsync(favoriteIds);

            // Act
            var result = await _lotteryService.GetUserFavoriteHousesCountAsync(_testUserId);

            // Assert
            result.Should().Be(2);
            _mockUserPreferencesService.Verify(s => s.GetFavoriteHouseIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetUserFavoriteHousesCountAsync_WithNoCache_QueriesDatabase()
        {
            // Arrange
            var favoriteIds = new List<Guid> { _testHouse.Id };
            _mockCache
                .Setup(c => c.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}", It.IsAny<bool>()))
                .ReturnsAsync((List<Guid>?)null);
            _mockUserPreferencesService
                .Setup(s => s.GetFavoriteHouseIdsAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favoriteIds);

            // Act
            var result = await _lotteryService.GetUserFavoriteHousesCountAsync(_testUserId);

            // Assert
            result.Should().Be(1);
            _mockUserPreferencesService.Verify(s => s.GetFavoriteHouseIdsAsync(_testUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddHouseToFavoritesAsync_WhenSignalRFails_StillReturnsTrue()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockClientProxy
                .Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SignalR error"));

            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            result.Should().BeTrue(); // Operation should succeed even if SignalR fails
        }

        [Fact]
        public async Task RemoveHouseFromFavoritesAsync_WhenCacheFails_StillReturnsTrue()
        {
            // Arrange
            _mockUserPreferencesService
                .Setup(s => s.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockCache
                .Setup(c => c.RemoveRecordAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Cache error"));

            // Act
            var result = await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse.Id);

            // Assert
            result.Should().BeTrue(); // Operation should succeed even if cache fails
        }

        public void Dispose()
        {
            _context?.Dispose();
            _authContext?.Dispose();
        }
    }
}

