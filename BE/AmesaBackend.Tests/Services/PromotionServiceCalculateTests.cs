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

public class PromotionServiceCalculateTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceCalculateTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldCalculatePercentageDiscount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PERCENT10",
            Title = "10% Discount",
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
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "PERCENT10",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(10); // 10% of 100
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldCalculateFixedDiscount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FIXED5",
            Title = "$5 Off",
            Type = "Discount",
            Value = 5,
            ValueType = "fixed",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "FIXED5",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(5); // Fixed $5
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldRespectMaxDiscount_ForPercentage()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MAX20",
            Title = "50% Max $20",
            Type = "Discount",
            Value = 50, // 50%
            ValueType = "percentage",
            MaxDiscountAmount = 20,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "MAX20",
            UserId = Guid.NewGuid(),
            Amount = 100 // 50% would be $50, but max is $20
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(20); // Capped at max
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldNotExceedPurchaseAmount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FIXED100",
            Title = "$100 Off",
            Type = "Discount",
            Value = 100,
            ValueType = "fixed",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "FIXED100",
            UserId = Guid.NewGuid(),
            Amount = 50 // Discount is $100 but purchase is only $50
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().BeLessThanOrEqualTo(50); // Should not exceed purchase amount
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleNullSearchParams()
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleEmptySearch()
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Search = ""
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleNullDescription()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NODESC",
            Title = "No Description",
            Description = null,
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Search = "NODESC"
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().Contain(p => p.Code == "NODESC");
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldHandleNullCode()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = null,
            Title = "No Code Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionByCodeAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludePromotionsAtUsageLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "LIMITREACHED",
            Title = "Limit Reached",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = 10,
            UsageCount = 10,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotContain(p => p.Code == "LIMITREACHED");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldIncludePromotionsWithoutUsageLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "UNLIMITED",
            Title = "Unlimited Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = null,
            UsageCount = 100,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().Contain(p => p.Code == "UNLIMITED");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldIncludePromotionsWithNullDates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NODATES",
            Title = "No Date Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            StartDate = null,
            EndDate = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().Contain(p => p.Code == "NODATES");
    }

    [Fact]
    public async Task GetPromotionAnalyticsAsync_ShouldHandleNullSearchParams()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "ANALYTICS",
            Title = "Analytics Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageCount = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
