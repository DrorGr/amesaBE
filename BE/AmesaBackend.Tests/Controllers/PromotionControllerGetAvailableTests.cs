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

public class PromotionControllerGetAvailableTests
{
    private readonly Mock<IPromotionService> _promotionServiceMock;
    private readonly Mock<ILogger<PromotionController>> _loggerMock;
    private readonly PromotionController _controller;

    public PromotionControllerGetAvailableTests()
    {
        _promotionServiceMock = new Mock<IPromotionService>();
        _loggerMock = new Mock<ILogger<PromotionController>>();
        _controller = new PromotionController(_promotionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldReturnPromotions_WhenAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var promotions = new List<PromotionDto>
        {
            new PromotionDto
            {
                Id = Guid.NewGuid(),
                Code = "TEST1",
                Name = "Test Promotion 1",
                Type = "Discount",
                Value = 10,
                ValueType = "percentage",
                IsActive = true
            },
            new PromotionDto
            {
                Id = Guid.NewGuid(),
                Code = "TEST2",
                Name = "Test Promotion 2",
                Type = "Discount",
                Value = 20,
                ValueType = "percentage",
                IsActive = true
            }
        };

        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, houseId))
            .ReturnsAsync(promotions);
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetAvailablePromotions(houseId);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldReturnEmptyList_WhenNoPromotionsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, houseId))
            .ReturnsAsync(new List<PromotionDto>());
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetAvailablePromotions(houseId);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldReturnPromotions_WhenHouseIdIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotions = new List<PromotionDto>
        {
            new PromotionDto
            {
                Id = Guid.NewGuid(),
                Code = "TEST",
                Name = "Test Promotion",
                Type = "Discount",
                Value = 10,
                ValueType = "percentage",
                IsActive = true
            }
        };

        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, null))
            .ReturnsAsync(promotions);
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetAvailablePromotions(null);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, houseId))
            .ThrowsAsync(new Exception("Service error"));
        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetAvailablePromotions(houseId);

        // Assert
        result.Should().NotBeNull();
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(500);
    }
}
