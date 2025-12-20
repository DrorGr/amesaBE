extern alias AuthApp;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AuthApp::AmesaBackend.Auth.Services;
using AmesaBackend.Shared.Caching;

namespace AmesaBackend.Tests.Controllers
{
    public class HousesFavoritesControllerTests
    {
        private readonly Mock<ILotteryService> _mockLotteryService;
        private readonly Mock<ILogger<HousesFavoritesController>> _mockLogger;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly HousesFavoritesController _controller;
        private readonly Guid _testUserId;
        private readonly Guid _testHouseId;

        public HousesFavoritesControllerTests()
        {
            _mockLotteryService = new Mock<ILotteryService>();
            _mockLogger = new Mock<ILogger<HousesFavoritesController>>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _testUserId = Guid.NewGuid();
            _testHouseId = Guid.NewGuid();

            _controller = new HousesFavoritesController(
                _mockLotteryService.Object,
                _mockLogger.Object,
                _mockRateLimitService.Object
            );

            // Setup authenticated user context
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        [Fact]
        public async Task GetFavoriteHouses_WithValidUser_ReturnsOk()
        {
            // Arrange
            var favoriteHouses = new List<HouseDto>
            {
                new HouseDto { Id = _testHouseId, Title = "Test House", Price = 500000 }
            };
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockLotteryService
                .Setup(s => s.GetUserFavoriteHousesAsync(_testUserId, 1, int.MaxValue, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favoriteHouses);
            _mockLotteryService
                .Setup(s => s.GetUserFavoriteHousesAsync(_testUserId, 1, 20, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(favoriteHouses);

            // Act
            var result = await _controller.GetFavoriteHouses();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<HouseDto>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetFavoriteHouses_WithRateLimitExceeded_Returns429()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GetFavoriteHouses();

            // Assert
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(429);
            var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<PagedResponse<HouseDto>>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error.Should().NotBeNull();
            apiResponse.Error!.Code.Should().Be("RATE_LIMIT_EXCEEDED");
        }

        [Fact]
        public async Task GetFavoriteHouses_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var allHouses = Enumerable.Range(1, 50)
                .Select(i => new HouseDto { Id = Guid.NewGuid(), Title = $"House {i}", Price = 100000 * i })
                .ToList();
            var paginatedHouses = allHouses.Take(20).ToList();

            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockLotteryService
                .Setup(s => s.GetUserFavoriteHousesAsync(_testUserId, 1, int.MaxValue, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(allHouses);
            _mockLotteryService
                .Setup(s => s.GetUserFavoriteHousesAsync(_testUserId, 1, 20, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paginatedHouses);

            // Act
            var result = await _controller.GetFavoriteHouses(page: 1, limit: 20);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<HouseDto>>>().Subject;
            apiResponse.Data!.Items.Should().HaveCount(20);
            apiResponse.Data.Total.Should().Be(50);
            apiResponse.Data.HasNext.Should().BeTrue();
        }

        [Fact]
        public async Task AddToFavorites_WithValidHouse_ReturnsOk()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockLotteryService
                .Setup(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddToFavorites(_testHouseId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<FavoriteHouseResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.HouseId.Should().Be(_testHouseId);
            apiResponse.Data.Added.Should().BeTrue();
        }

        [Fact]
        public async Task AddToFavorites_WithRateLimitExceeded_Returns429()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AddToFavorites(_testHouseId);

            // Assert
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(429);
        }

        [Fact]
        public async Task AddToFavorites_WithInvalidGuid_ReturnsBadRequest()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddToFavorites(Guid.Empty);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<FavoriteHouseResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Error.Should().NotBeNull();
            apiResponse.Error!.Code.Should().Be("VALIDATION_ERROR");
        }

        [Fact]
        public async Task AddToFavorites_WhenServiceReturnsFalse_ReturnsBadRequest()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockLotteryService
                .Setup(s => s.AddHouseToFavoritesAsync(_testUserId, _testHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AddToFavorites(_testHouseId);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<FavoriteHouseResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveFromFavorites_WithValidHouse_ReturnsOk()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockLotteryService
                .Setup(s => s.RemoveHouseFromFavoritesAsync(_testUserId, _testHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveFromFavorites(_testHouseId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<FavoriteHouseResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.HouseId.Should().Be(_testHouseId);
            apiResponse.Data.Added.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveFromFavorites_WithRateLimitExceeded_Returns429()
        {
            // Arrange
            _mockRateLimitService
                .Setup(s => s.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RemoveFromFavorites(_testHouseId);

            // Assert
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(429);
        }

        [Fact]
        public async Task GetFavoriteHousesCount_WithValidUser_ReturnsCount()
        {
            // Arrange
            _mockLotteryService
                .Setup(s => s.GetUserFavoriteHousesCountAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            // Act
            var result = await _controller.GetFavoriteHousesCount();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            apiResponse.Success.Should().BeTrue();
            // Note: The response data is an anonymous object with count property
            // We can't directly assert on it, but we can verify the service was called
            _mockLotteryService.Verify(s => s.GetUserFavoriteHousesCountAsync(_testUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetFavoriteHouses_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal() // Empty principal = unauthenticated
                }
            };

            // Act
            var result = await _controller.GetFavoriteHouses();

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<PagedResponse<HouseDto>>>().Subject;
            apiResponse.Success.Should().BeFalse();
        }
    }
}

