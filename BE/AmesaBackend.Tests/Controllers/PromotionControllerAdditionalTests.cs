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

public class PromotionControllerAdditionalTests
{
    private readonly Mock<IPromotionService> _promotionServiceMock;
    private readonly Mock<ILogger<PromotionController>> _loggerMock;
    private readonly PromotionController _controller;

    public PromotionControllerAdditionalTests()
    {
        _promotionServiceMock = new Mock<IPromotionService>();
        _loggerMock = new Mock<ILogger<PromotionController>>();
        _controller = new PromotionController(_promotionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdatePromotion_ShouldReturn500_WhenPromotionNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdatePromotionRequest
        {
            Name = "Updated Name"
        };

        _promotionServiceMock
            .Setup(x => x.UpdatePromotionAsync(id, request))
            .ThrowsAsync(new KeyNotFoundException("Promotion not found"));

        // Act
        var result = await _controller.UpdatePromotion(id, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdatePromotion_ShouldReturnSuccess_WhenRequestValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdatePromotionRequest
        {
            Name = "Updated Name"
        };

        var updatedPromotion = new PromotionDto
        {
            Id = id,
            Code = "TEST",
            Name = "Updated Name",
            Type = "Discount",
            Value = 10,
            IsActive = true
        };

        _promotionServiceMock
            .Setup(x => x.UpdatePromotionAsync(id, request))
            .ReturnsAsync(updatedPromotion);

        // Act
        var result = await _controller.UpdatePromotion(id, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeletePromotion_ShouldReturnSuccess_WhenPromotionNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _promotionServiceMock
            .Setup(x => x.DeletePromotionAsync(id))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePromotion(id);

        // Assert
        // Controller returns Ok even when promotion not found (returns false)
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<bool>;
        response!.Data.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePromotion_ShouldReturnSuccess_WhenPromotionDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();

        _promotionServiceMock
            .Setup(x => x.DeletePromotionAsync(id))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePromotion(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<bool>;
        response!.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotion_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var request = new ValidatePromotionRequest
        {
            Code = "TEST10",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // No user claims set up

        // Act
        var result = await _controller.ValidatePromotion(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().BeOneOf(401, 500);
    }

    [Fact]
    public async Task ValidatePromotion_ShouldReturnValidationResponse_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ValidatePromotionRequest
        {
            Code = "TEST10",
            UserId = userId,
            Amount = 100
        };

        var validationResponse = new PromotionValidationResponse
        {
            IsValid = true,
            Message = "Promotion is valid",
            DiscountAmount = 10
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _promotionServiceMock
            .Setup(x => x.ValidatePromotionAsync(It.Is<ValidatePromotionRequest>(r => r.UserId == userId)))
            .ReturnsAsync(validationResponse);

        // Act
        var result = await _controller.ValidatePromotion(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<PromotionValidationResponse>;
        response!.Data.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserPromotionHistory_ShouldReturnForbid_WhenUserMismatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, otherUserId);

        // Act
        var result = await _controller.GetUserPromotionHistory(userId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetUserPromotionHistory_ShouldReturnHistory_WhenUserMatches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var history = new List<PromotionUsageDto>
        {
            new PromotionUsageDto
            {
                Id = Guid.NewGuid(),
                PromotionId = Guid.NewGuid(),
                PromotionCode = "TEST10",
                UserId = userId,
                DiscountAmount = 10,
                UsedAt = DateTime.UtcNow
            }
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _promotionServiceMock
            .Setup(x => x.GetUserPromotionHistoryAsync(userId))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.GetUserPromotionHistory(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<List<PromotionUsageDto>>;
        response!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldReturnPromotions_WhenUserAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotions = new List<PromotionDto>
        {
            new PromotionDto
            {
                Id = Guid.NewGuid(),
                Code = "TEST10",
                Name = "Test Promotion",
                Type = "Discount",
                Value = 10,
                IsActive = true
            }
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, null))
            .ReturnsAsync(promotions);

        // Act
        var result = await _controller.GetAvailablePromotions(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<List<PromotionDto>>;
        response!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAvailablePromotions_ShouldFilterByHouseId_WhenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId = Guid.NewGuid();
        var promotions = new List<PromotionDto>
        {
            new PromotionDto
            {
                Id = Guid.NewGuid(),
                Code = "HOUSE1",
                Name = "House Promotion",
                Type = "Discount",
                Value = 10,
                IsActive = true
            }
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _promotionServiceMock
            .Setup(x => x.GetAvailablePromotionsAsync(userId, houseId))
            .ReturnsAsync(promotions);

        // Act
        var result = await _controller.GetAvailablePromotions(houseId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _promotionServiceMock.Verify(x => x.GetAvailablePromotionsAsync(userId, houseId), Times.Once);
    }

    [Fact]
    public async Task GetPromotionStats_ShouldReturnStats_WhenPromotionExists()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var stats = new PromotionAnalyticsDto
        {
            PromotionId = promotionId,
            Code = "TEST10",
            Name = "Test Promotion",
            TotalUsage = 10,
            UniqueUsers = 5,
            TotalDiscountAmount = 100
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionUsageStatsAsync(promotionId))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetPromotionStats(promotionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<PromotionAnalyticsDto>;
        response!.Data.TotalUsage.Should().Be(10);
    }

    [Fact]
    public async Task GetPromotionStats_ShouldReturn500_WhenPromotionNotFound()
    {
        // Arrange
        var promotionId = Guid.NewGuid();

        _promotionServiceMock
            .Setup(x => x.GetPromotionUsageStatsAsync(promotionId))
            .ThrowsAsync(new KeyNotFoundException("Promotion not found"));

        // Act
        var result = await _controller.GetPromotionStats(promotionId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPromotionAnalytics_ShouldReturnAnalytics_WhenPromotionsExist()
    {
        // Arrange
        var analytics = new List<PromotionAnalyticsDto>
        {
            new PromotionAnalyticsDto
            {
                PromotionId = Guid.NewGuid(),
                Code = "PROMO1",
                Name = "Promotion 1",
                TotalUsage = 10,
                UniqueUsers = 5,
                TotalDiscountAmount = 100
            },
            new PromotionAnalyticsDto
            {
                PromotionId = Guid.NewGuid(),
                Code = "PROMO2",
                Name = "Promotion 2",
                TotalUsage = 5,
                UniqueUsers = 3,
                TotalDiscountAmount = 50
            }
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionAnalyticsAsync(null))
            .ReturnsAsync(analytics);

        // Act
        var result = await _controller.GetPromotionAnalytics(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<List<PromotionAnalyticsDto>>;
        response!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPromotionAnalytics_ShouldFilterBySearchParams_WhenProvided()
    {
        // Arrange
        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Type = "Discount"
        };

        var analytics = new List<PromotionAnalyticsDto>
        {
            new PromotionAnalyticsDto
            {
                PromotionId = Guid.NewGuid(),
                Code = "DISCOUNT10",
                Name = "Discount Promotion",
                TotalUsage = 10,
                UniqueUsers = 5,
                TotalDiscountAmount = 100
            }
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionAnalyticsAsync(searchParams))
            .ReturnsAsync(analytics);

        // Act
        var result = await _controller.GetPromotionAnalytics(searchParams);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _promotionServiceMock.Verify(x => x.GetPromotionAnalyticsAsync(searchParams), Times.Once);
    }

    [Fact]
    public async Task CreatePromotion_ShouldReturnCreated_WhenPromotionCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreatePromotionRequest
        {
            Name = "New Promotion",
            Code = "NEW10",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        var createdPromotion = new PromotionDto
        {
            Id = Guid.NewGuid(),
            Code = "NEW10",
            Name = "New Promotion",
            Type = "Discount",
            Value = 10,
            IsActive = true
        };

        AmesaBackend.Tests.TestHelpers.TestHelpers.SetupUserClaims(_controller, userId);

        _promotionServiceMock
            .Setup(x => x.CreatePromotionAsync(request, userId))
            .ReturnsAsync(createdPromotion);

        // Act
        var result = await _controller.CreatePromotion(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetPromotionByCode_ShouldReturnNotFound_WhenPromotionNotFound()
    {
        // Arrange
        var code = "INVALID";

        _promotionServiceMock
            .Setup(x => x.GetPromotionByCodeAsync(code))
            .ReturnsAsync((PromotionDto?)null);

        // Act
        var result = await _controller.GetPromotionByCode(code);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPromotionByCode_ShouldReturnPromotion_WhenFound()
    {
        // Arrange
        var code = "TEST10";
        var promotion = new PromotionDto
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            IsActive = true
        };

        _promotionServiceMock
            .Setup(x => x.GetPromotionByCodeAsync(code))
            .ReturnsAsync(promotion);

        // Act
        var result = await _controller.GetPromotionByCode(code);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as StandardApiResponse<PromotionDto>;
        response!.Data.Code.Should().Be(code);
    }
}
