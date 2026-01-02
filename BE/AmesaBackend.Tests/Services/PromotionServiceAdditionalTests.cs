using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Models;
using AmesaBackend.Shared.Events;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class PromotionServiceAdditionalTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceAdditionalTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserPromotionHistoryAsync_ShouldReturnEmptyList_WhenUserHasNoHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPromotionHistoryAsync_ShouldReturnHistoryOrderedByDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "TEST10",
            Title = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);

        var usage1 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-2)
        };

        var usage2 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 20,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.UserPromotions.AddRange(usage1, usage2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(u => u.UsedAt);
    }

    [Fact]
    public async Task GetPromotionUsageStatsAsync_ShouldReturnStats_WhenPromotionExists()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "TEST10",
            Title = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = 100,
            UsageCount = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);

        var usage1 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-2)
        };

        var usage2 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 20,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.UserPromotions.AddRange(usage1, usage2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionUsageStatsAsync(promotionId);

        // Assert
        result.Should().NotBeNull();
        result.PromotionId.Should().Be(promotionId);
        result.TotalUsage.Should().Be(2);
        result.UniqueUsers.Should().Be(2);
        result.TotalDiscountAmount.Should().Be(30);
    }

    [Fact]
    public async Task GetPromotionUsageStatsAsync_ShouldThrowException_WhenPromotionNotFound()
    {
        // Arrange
        var promotionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetPromotionUsageStatsAsync(promotionId));
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldReturnAnalytics_WhenPromotionsExist()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PROMO1",
            Title = "Promotion 1",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageCount = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promotion2 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PROMO2",
            Title = "Promotion 2",
            Type = "Cashback",
            Value = 5,
            ValueType = "percentage",
            UsageCount = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(promotion1, promotion2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldFilterBySearchParams()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "DISCOUNT10",
            Title = "Discount Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageCount = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promotion2 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "CASHBACK5",
            Title = "Cashback Promotion",
            Type = "Cashback",
            Value = 5,
            ValueType = "percentage",
            UsageCount = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(promotion1, promotion2);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Type = "Discount"
        };

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenUserAlreadyUsedPromotion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "ONETIME",
            Title = "One Time Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);

        var existingUsage = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.UserPromotions.Add(existingUsage);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "ONETIME",
            UserId = userId,
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("already been used");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenHouseNotApplicable()
    {
        // Arrange
        var houseId = Guid.NewGuid();
        var otherHouseId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "HOUSE1",
            Title = "House Specific",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { houseId },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "HOUSE1",
            UserId = Guid.NewGuid(),
            Amount = 100,
            HouseId = otherHouseId
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not applicable");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludeExpiredPromotions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "ACTIVE",
            Title = "Active Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expiredPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "EXPIRED",
            Title = "Expired Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(activePromotion, expiredPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().Contain(p => p.Code == "ACTIVE");
        result.Should().NotContain(p => p.Code == "EXPIRED");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludeNotStartedPromotions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "ACTIVE",
            Title = "Active Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var futurePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FUTURE",
            Title = "Future Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(activePromotion, futurePromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().Contain(p => p.Code == "ACTIVE");
        result.Should().NotContain(p => p.Code == "FUTURE");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
