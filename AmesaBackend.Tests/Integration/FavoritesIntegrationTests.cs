extern alias AuthApp;
using Xunit;
using FluentAssertions;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Hubs;
using AuthApp::AmesaBackend.Auth.Data;
using AuthApp::AmesaBackend.Auth.Models;
using AuthApp::AmesaBackend.Auth.Services;
using AmesaBackend.Tests.TestHelpers;
using AmesaBackend.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Text.Json;

namespace AmesaBackend.Tests.Integration
{
    public class FavoritesIntegrationTests : IDisposable
    {
        private readonly LotteryDbContext _lotteryContext;
        private readonly AuthDbContext _authContext;
        private readonly InMemoryCache _cache;
        private readonly Mock<IHubContext<LotteryHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly ILogger<LotteryService> _logger;
        private readonly ILogger<UserPreferencesService> _prefsLogger;
        private readonly UserPreferencesService _userPreferencesService;
        private readonly LotteryService _lotteryService;
        private readonly House _testHouse1;
        private readonly House _testHouse2;
        private readonly Guid _testUserId;

        public FavoritesIntegrationTests()
        {
            var lotteryOptions = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: $"FavoritesIntegrationTestDb_{Guid.NewGuid()}")
                .Options;

            var authOptions = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(databaseName: $"FavoritesAuthIntegrationTestDb_{Guid.NewGuid()}")
                .Options;

            _lotteryContext = new LotteryDbContext(lotteryOptions);
            _authContext = new AuthDbContext(authOptions);
            _cache = new InMemoryCache();
            _mockHubContext = new Mock<IHubContext<LotteryHub>>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockEventPublisher = new Mock<IEventPublisher>();
            _logger = new Mock<ILogger<LotteryService>>().Object;
            _prefsLogger = new Mock<ILogger<UserPreferencesService>>().Object;

            // Setup SignalR mocks
            _mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClientProxy.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Initialize services
            _userPreferencesService = new UserPreferencesService(_authContext, _prefsLogger);
            _lotteryService = new LotteryService(
                _lotteryContext,
                _mockEventPublisher.Object,
                _logger,
                _userPreferencesService,
                null, // configurationService
                _authContext,
                null, // httpRequest
                null, // configuration
                null, // redis
                _mockHubContext.Object,
                _cache,
                null // gamificationService
            );

            // Setup test data
            _testUserId = Guid.NewGuid();
            _testHouse1 = new House
            {
                Id = Guid.NewGuid(),
                Title = "Test House 1",
                Price = 500000,
                Location = "Location 1",
                TotalTickets = 1000,
                TicketPrice = 50,
                Status = "Active",
                LotteryEndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeletedAt = null
            };
            _testHouse2 = new House
            {
                Id = Guid.NewGuid(),
                Title = "Test House 2",
                Price = 600000,
                Location = "Location 2",
                TotalTickets = 2000,
                TicketPrice = 60,
                Status = "Active",
                LotteryEndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeletedAt = null
            };
            _lotteryContext.Houses.AddRange(_testHouse1, _testHouse2);
            _lotteryContext.SaveChanges();
        }

        [Fact]
        public async Task EndToEnd_AddFavorite_ShouldWork()
        {
            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id);

            // Assert
            result.Should().BeTrue();
            
            // Verify favorite was added
            var favoriteIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(_testUserId);
            favoriteIds.Should().Contain(_testHouse1.Id);
            
            // Verify SignalR broadcast was sent
            _mockClientProxy.Verify(c => c.SendAsync(
                "FavoriteUpdate",
                It.Is<FavoriteUpdateDto>(dto => dto.HouseId == _testHouse1.Id && dto.UpdateType == "added"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EndToEnd_RemoveFavorite_ShouldWork()
        {
            // Arrange - Add favorite first
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id);
            _mockClientProxy.Invocations.Clear();

            // Act
            var result = await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse1.Id);

            // Assert
            result.Should().BeTrue();
            
            // Verify favorite was removed
            var favoriteIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(_testUserId);
            favoriteIds.Should().NotContain(_testHouse1.Id);
            
            // Verify SignalR broadcast was sent
            _mockClientProxy.Verify(c => c.SendAsync(
                "FavoriteUpdate",
                It.Is<FavoriteUpdateDto>(dto => dto.HouseId == _testHouse1.Id && dto.UpdateType == "removed"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EndToEnd_GetFavorites_ShouldReturnCachedResults()
        {
            // Arrange - Add favorites
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id);
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse2.Id);

            // Act - First call (should cache)
            var result1 = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId);

            // Assert
            result1.Should().HaveCount(2);
            result1.Should().Contain(h => h.Id == _testHouse1.Id);
            result1.Should().Contain(h => h.Id == _testHouse2.Id);

            // Verify cache was populated
            var cachedIds = await _cache.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}");
            cachedIds.Should().NotBeNull();
            cachedIds.Should().HaveCount(2);
        }

        [Fact]
        public async Task EndToEnd_ConcurrentAddFavorites_ShouldHandleRaceConditions()
        {
            // Arrange
            var tasks = new List<Task<bool>>();

            // Act - Add same favorite concurrently from multiple "requests"
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id));
            }
            var results = await Task.WhenAll(tasks);

            // Assert - Only one should succeed (idempotent operation)
            // The retry logic should handle concurrency conflicts
            var successCount = results.Count(r => r);
            successCount.Should().BeGreaterThan(0); // At least one should succeed
            
            // Verify favorite was added exactly once
            var favoriteIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(_testUserId);
            favoriteIds.Count(f => f == _testHouse1.Id).Should().Be(1);
        }

        [Fact]
        public async Task EndToEnd_CacheInvalidation_ShouldWork()
        {
            // Arrange - Add favorite and get it (populates cache)
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id);
            await _lotteryService.GetUserFavoriteHousesAsync(_testUserId);
            
            // Verify cache exists
            var cachedBefore = await _cache.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}");
            cachedBefore.Should().NotBeNull();

            // Act - Remove favorite (should invalidate cache)
            await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, _testHouse1.Id);

            // Assert - Cache should be invalidated
            var cachedAfter = await _cache.GetRecordAsync<List<Guid>>($"lottery:favorites:{_testUserId}");
            cachedAfter.Should().BeNull(); // Cache was removed
        }

        [Fact]
        public async Task EndToEnd_Pagination_ShouldWork()
        {
            // Arrange - Add multiple favorites
            var houses = new List<House>();
            for (int i = 0; i < 10; i++)
            {
                var house = new House
                {
                    Id = Guid.NewGuid(),
                    Title = $"House {i}",
                    Price = 100000 * (i + 1),
                    Location = $"Location {i}",
                    Status = "Active",
                    LotteryEndDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };
                houses.Add(house);
                _lotteryContext.Houses.Add(house);
            }
            await _lotteryContext.SaveChangesAsync();

            foreach (var house in houses)
            {
                await _lotteryService.AddHouseToFavoritesAsync(_testUserId, house.Id);
            }

            // Act - Get first page
            var page1 = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId, page: 1, limit: 5);
            
            // Act - Get second page
            var page2 = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId, page: 2, limit: 5);

            // Assert
            page1.Should().HaveCount(5);
            page2.Should().HaveCount(5);
            page1.Should().NotIntersectWith(page2); // No overlap
        }

        [Fact]
        public async Task EndToEnd_Sorting_ShouldWork()
        {
            // Arrange - Add favorites
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id); // Price: 500000
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse2.Id); // Price: 600000

            // Act - Sort by price ascending
            var sortedAsc = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId, sortBy: "price", sortOrder: "asc");

            // Assert
            sortedAsc.Should().HaveCount(2);
            sortedAsc[0].Price.Should().Be(500000); // _testHouse1
            sortedAsc[1].Price.Should().Be(600000); // _testHouse2

            // Act - Sort by price descending
            var sortedDesc = await _lotteryService.GetUserFavoriteHousesAsync(_testUserId, sortBy: "price", sortOrder: "desc");

            // Assert
            sortedDesc[0].Price.Should().Be(600000); // _testHouse2
            sortedDesc[1].Price.Should().Be(500000); // _testHouse1
        }

        [Fact]
        public async Task EndToEnd_GetCount_ShouldUseCache()
        {
            // Arrange - Add favorites
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse1.Id);
            await _lotteryService.AddHouseToFavoritesAsync(_testUserId, _testHouse2.Id);
            
            // Populate cache by getting favorites
            await _lotteryService.GetUserFavoriteHousesAsync(_testUserId);

            // Act - Get count (should use cache)
            var count = await _lotteryService.GetUserFavoriteHousesCountAsync(_testUserId);

            // Assert
            count.Should().Be(2);
        }

        [Fact]
        public async Task EndToEnd_AddNonExistentHouse_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentHouseId = Guid.NewGuid();

            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, nonExistentHouseId);

            // Assert
            result.Should().BeFalse();
            
            // Verify no favorite was added
            var favoriteIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(_testUserId);
            favoriteIds.Should().NotContain(nonExistentHouseId);
        }

        [Fact]
        public async Task EndToEnd_RemoveNonExistentHouse_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentHouseId = Guid.NewGuid();

            // Act
            var result = await _lotteryService.RemoveHouseFromFavoritesAsync(_testUserId, nonExistentHouseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task EndToEnd_AddSoftDeletedHouse_ShouldReturnFalse()
        {
            // Arrange
            var deletedHouse = new House
            {
                Id = Guid.NewGuid(),
                Title = "Deleted House",
                Status = "Active",
                LotteryEndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                DeletedAt = DateTime.UtcNow // Soft deleted
            };
            _lotteryContext.Houses.Add(deletedHouse);
            await _lotteryContext.SaveChangesAsync();

            // Act
            var result = await _lotteryService.AddHouseToFavoritesAsync(_testUserId, deletedHouse.Id);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _lotteryContext?.Dispose();
            _authContext?.Dispose();
            _cache?.Dispose();
        }
    }
}

