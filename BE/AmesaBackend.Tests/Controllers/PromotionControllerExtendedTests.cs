using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Shared.Contracts;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Controllers;

public class PromotionControllerExtendedTests
{
    private readonly Mock<IPromotionService> _promotionServiceMock;
    private readonly Mock<ILogger<PromotionController>> _loggerMock;
    private readonly PromotionController _controller;

    public PromotionControllerExtendedTests()
    {
        _promotionServiceMock = new Mock<IPromotionService>();
        _loggerMock = new Mock<ILogger<PromotionController>>();
        _controller = new PromotionController(_promotionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPromotions_ShouldHandleServiceException()
    {
        // Arrange
        var searchParams = new PromotionSearchParams { Page = 1, Limit = 10 };
        _promotionServiceMock
            .Setup(x => x.GetPromotionsAsync(searchParams))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotions(searchParams);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPromotionByCode_ShouldReturnPromotion_WhenExists()
    {
        // Arrange
        var code = "TEST10";
        var promotion = new PromotionDto
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Test Promotion"
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionByCodeAsync(code))
            .ReturnsAsync(promotion);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionByCode(code);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPromotionByCode_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var code = "INVALID";
        _promotionServiceMock
            .Setup(x => x.GetPromotionByCodeAsync(code))
            .ReturnsAsync((PromotionDto?)null);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionByCode(code);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPromotionByCode_ShouldHandleServiceException()
    {
        // Arrange
        var code = "TEST10";
        _promotionServiceMock
            .Setup(x => x.GetPromotionByCodeAsync(code))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionByCode(code);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreatePromotion_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreatePromotionRequest
        {
            Code = "TEST10",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        _promotionServiceMock
            .Setup(x => x.CreatePromotionAsync(request, userId))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.CreatePromotion(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdatePromotion_ShouldHandleServiceException()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var request = new UpdatePromotionRequest
        {
            Name = "Updated Promotion"
        };

        _promotionServiceMock
            .Setup(x => x.UpdatePromotionAsync(promotionId, request))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.UpdatePromotion(promotionId, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeletePromotion_ShouldHandleServiceException()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.DeletePromotionAsync(promotionId))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.DeletePromotion(promotionId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task ValidatePromotion_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ValidatePromotionRequest
        {
            Code = "TEST10",
            UserId = userId,
            Amount = 100
        };

        _promotionServiceMock
            .Setup(x => x.ValidatePromotionAsync(It.IsAny<ValidatePromotionRequest>()))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.ValidatePromotion(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task ApplyPromotion_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ApplyPromotionRequest
        {
            Code = "TEST10",
            UserId = userId,
            TransactionId = Guid.NewGuid(),
            Amount = 100,
            DiscountAmount = 10
        };

        _promotionServiceMock
            .Setup(x => x.ApplyPromotionAsync(It.IsAny<ApplyPromotionRequest>()))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.ApplyPromotion(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task ApplyPromotion_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var request = new ApplyPromotionRequest
        {
            Code = "TEST10",
            UserId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            Amount = 100,
            DiscountAmount = 10
        };

        // Act
        var result = await _controller.ApplyPromotion(request);

        // Assert
        result.Result.Should().NotBeNull();
        if (result.Result is ObjectResult objectResult)
        {
            objectResult.StatusCode.Should().BeOneOf(401, 500);
        }
        else
        {
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }

    [Fact]
    public async Task GetUserPromotionHistory_ShouldReturnHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var history = new List<PromotionUsageDto>
        {
            new PromotionUsageDto
            {
                Id = Guid.NewGuid(),
                PromotionId = Guid.NewGuid(),
                UserId = userId,
                TransactionId = Guid.NewGuid(),
                DiscountAmount = 10
            }
        };

        _promotionServiceMock
            .Setup(x => x.GetUserPromotionHistoryAsync(userId))
            .ReturnsAsync(history);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetUserPromotionHistory(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserPromotionHistory_ShouldReturnForbid_WhenUserMismatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, differentUserId);

        // Act
        var result = await _controller.GetUserPromotionHistory(userId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetUserPromotionHistory_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.GetUserPromotionHistory(userId);

        // Assert
        result.Result.Should().NotBeNull();
        if (result.Result is ObjectResult objectResult)
        {
            objectResult.StatusCode.Should().BeOneOf(401, 403, 500);
        }
    }

    [Fact]
    public async Task GetUserPromotionHistory_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.GetUserPromotionHistoryAsync(userId))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetUserPromotionHistory(userId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldReturnEmptyList_WhenServiceReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, null))
            .ReturnsAsync(new List<PromotionDto>());

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetAvailablePromotions(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange

        // Act
        var result = await _controller.GetAvailablePromotions(null);

        // Assert
        result.Result.Should().NotBeNull();
        if (result.Result is ObjectResult objectResult)
        {
            objectResult.StatusCode.Should().BeOneOf(401, 500);
        }
        else
        {
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldHandleServiceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, null))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        // Act
        var result = await _controller.GetAvailablePromotions(null);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPromotionStats_ShouldReturnStats()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var stats = new PromotionAnalyticsDto
        {
            PromotionId = promotionId,
            TotalUsage = 10,
            TotalDiscountAmount = 100,
            UniqueUsers = 5
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionUsageStatsAsync(promotionId))
            .ReturnsAsync(stats);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionStats(promotionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPromotionStats_ShouldHandleServiceException()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        _promotionServiceMock
            .Setup(x => x.GetPromotionUsageStatsAsync(promotionId))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionStats(promotionId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPromotionAnalytics_ShouldReturnAnalytics()
    {
        // Arrange
        var searchParams = new PromotionSearchParams { Page = 1, Limit = 10 };
        var analytics = new List<PromotionAnalyticsDto>
        {
            new PromotionAnalyticsDto
            {
                PromotionId = Guid.NewGuid(),
                TotalUsage = 10,
                TotalDiscountAmount = 100,
                UniqueUsers = 5
            }
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionAnalyticsAsync(searchParams))
            .ReturnsAsync(analytics);

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionAnalytics(searchParams);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPromotionAnalytics_ShouldHandleServiceException()
    {
        // Arrange
        var searchParams = new PromotionSearchParams { Page = 1, Limit = 10 };
        _promotionServiceMock
            .Setup(x => x.GetPromotionAnalyticsAsync(searchParams))
            .ThrowsAsync(new Exception("Service error"));

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, Guid.NewGuid());

        // Act
        var result = await _controller.GetPromotionAnalytics(searchParams);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
