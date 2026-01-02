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

public class PromotionServiceAnalyticsTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceAnalyticsTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldReturnAnalytics_WhenNoSearchParams()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = 100,
            UsageCount = 5,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(a => a.PromotionId == promotion.Id);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldFilterByIsActive()
    {
        // Arrange
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

        var inactivePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "INACTIVE",
            Title = "Inactive Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = false,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(activePromotion, inactivePromotion);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            IsActive = true
        };

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(a => a.PromotionId == activePromotion.Id);
        result.Should().NotContain(a => a.PromotionId == inactivePromotion.Id);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldFilterByType()
    {
        // Arrange
        var discountPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "DISCOUNT",
            Title = "Discount Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var bonusPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "BONUS",
            Title = "Bonus Promotion",
            Type = "Bonus",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(discountPromotion, bonusPromotion);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Type = "Discount"
        };

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(a => a.PromotionId == discountPromotion.Id);
        result.Should().NotContain(a => a.PromotionId == bonusPromotion.Id);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldFilterByStartDate()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "SUMMER",
            Title = "Summer Sale",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promotion2 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "WINTER",
            Title = "Winter Sale",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(promotion1, promotion2);
        await _context.SaveChangesAsync();

        // Act - Get all analytics
        var result = await _service.GetPromotionAnalyticsAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(a => a.PromotionId == promotion1.Id);
        result.Should().Contain(a => a.PromotionId == promotion2.Id);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldCalculateTotalUsage()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = 100,
            UsageCount = 2,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);

        var userPromotion1 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            PromotionId = promotion.Id,
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };

        var userPromotion2 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            PromotionId = promotion.Id,
            DiscountAmount = 20,
            UsedAt = DateTime.UtcNow
        };

        _context.UserPromotions.AddRange(userPromotion1, userPromotion2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(null);

        // Assert
        result.Should().NotBeNull();
        var analytics = result.FirstOrDefault(a => a.PromotionId == promotion.Id);
        analytics.Should().NotBeNull();
        analytics!.TotalUsage.Should().Be(2);
        analytics.UniqueUsers.Should().Be(2);
        analytics.TotalDiscountAmount.Should().Be(30);
        analytics.AverageDiscountAmount.Should().Be(15);
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldReturnEmptyList_WhenNoPromotions()
    {
        // Act
        var result = await _service.GetPromotionAnalyticsAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
