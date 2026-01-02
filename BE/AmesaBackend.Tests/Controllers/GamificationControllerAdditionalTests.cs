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

public class GamificationControllerAdditionalTests
{
    private readonly Mock<IGamificationService> _gamificationServiceMock;
    private readonly Mock<ILogger<GamificationController>> _loggerMock;
    private readonly GamificationController _controller;

    public GamificationControllerAdditionalTests()
    {
        _gamificationServiceMock = new Mock<IGamificationService>();
        _loggerMock = new Mock<ILogger<GamificationController>>();
        _controller = new GamificationController(_gamificationServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturnData_WhenUserAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gamificationData = new UserGamificationDto
        {
            UserId = userId.ToString(),
            TotalPoints = 1000,
            CurrentLevel = 5,
            CurrentTier = "Silver",
            CurrentStreak = 10,
            LongestStreak = 15,
            LastEntryDate = DateTime.UtcNow,
            RecentAchievements = new List<AchievementDto>
            {
                new AchievementDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "First Entry",
                    Description = "Unlocked First Entry achievement",
                    Icon = "🎟️",
                    UnlockedAt = DateTime.UtcNow,
                    Category = "EntryBased"
                }
            }
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _gamificationServiceMock
            .Setup(x => x.GetUserGamificationAsync(userId))
            .ReturnsAsync(gamificationData);

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGamificationData_ShouldReturnDefaultData_WhenNoGamificationRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var defaultData = new UserGamificationDto
        {
            UserId = userId.ToString(),
            TotalPoints = 0,
            CurrentLevel = 1,
            CurrentTier = "Bronze",
            CurrentStreak = 0,
            LongestStreak = 0,
            LastEntryDate = null,
            RecentAchievements = new List<AchievementDto>()
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _gamificationServiceMock
            .Setup(x => x.GetUserGamificationAsync(userId))
            .ReturnsAsync(defaultData);

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGamificationData_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _gamificationServiceMock
            .Setup(x => x.GetUserGamificationAsync(userId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetGamificationData();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
