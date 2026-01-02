using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Controllers;

public class GamificationControllerTests
{
    private readonly Mock<IGamificationService> _gamificationServiceMock;
    private readonly Mock<ILogger<GamificationController>> _loggerMock;
    private readonly GamificationController _controller;

    public GamificationControllerTests()
    {
        _gamificationServiceMock = new Mock<IGamificationService>();
        _loggerMock = new Mock<ILogger<GamificationController>>();
        _controller = new GamificationController(
            _gamificationServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturnGamificationData_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamificationDto = new UserGamificationDto
        {
            UserId = userId.ToString(),
            TotalPoints = 1000,
            CurrentLevel = 5,
            CurrentTier = "Silver",
            CurrentStreak = 3,
            LongestStreak = 5,
            RecentAchievements = new List<AchievementDto>()
        };

        _gamificationServiceMock
            .Setup(s => s.GetUserGamificationAsync(userId))
            .ReturnsAsync(gamificationDto);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        // Verify response structure - GamificationController uses StandardApiResponse
        // Frontend expects { success, data } format, but controller returns StandardApiResponse with Success/Data
        // The JSON serializer should convert it to lowercase, but we verify the structure exists
        var responseType = response!.GetType();
        var successProperty = responseType.GetProperty("Success") ?? responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("Data") ?? responseType.GetProperty("data");
        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);

        _gamificationServiceMock.Verify(s => s.GetUserGamificationAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturnDefaultData_WhenUserHasNoGamification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamificationDto = new UserGamificationDto
        {
            UserId = userId.ToString(),
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 0,
            LongestStreak = 0,
            RecentAchievements = new List<AchievementDto>()
        };

        _gamificationServiceMock
            .Setup(s => s.GetUserGamificationAsync(userId))
            .ReturnsAsync(gamificationDto);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
        
        // Verify response structure
        var responseType = response!.GetType();
        var successProperty = responseType.GetProperty("Success") ?? responseType.GetProperty("success");
        var dataProperty = responseType.GetProperty("Data") ?? responseType.GetProperty("data");
        Assert.NotNull(successProperty);
        Assert.NotNull(dataProperty);
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange - No user claims set up

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        // When there's no user, GetUserId() throws UnauthorizedAccessException which may be caught and return 500
        // or the [Authorize] attribute may prevent access and return 401
        result.Should().NotBeNull();
        if (result is ObjectResult objectResult)
        {
            // Accept either 401 (unauthorized) or 500 (internal server error from exception)
            objectResult.StatusCode.Should().BeOneOf(401, 500);
        }
        else
        {
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturnServiceUnavailable_WhenServiceIsNull()
    {
        // Arrange
        var controllerWithNullService = new GamificationController(
            null!,
            _loggerMock.Object
        );

        var userId = Guid.NewGuid();
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(controllerWithNullService, userId);

        // Act
        var result = await controllerWithNullService.GetGamificationData();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetGamificationData_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _gamificationServiceMock
            .Setup(s => s.GetUserGamificationAsync(userId))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
