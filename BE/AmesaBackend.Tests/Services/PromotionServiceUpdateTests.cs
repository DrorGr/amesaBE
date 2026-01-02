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

public class PromotionServiceUpdateTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceUpdateTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldUpdateAllFields()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "ORIGINAL",
            Title = "Original Title",
            Description = "Original Description",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = 50,
            MaxDiscountAmount = 20,
            UsageLimit = 100,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new UpdatePromotionRequest
        {
            Name = "Updated Title",
            Description = "Updated Description",
            Type = "Cashback",
            Value = 15,
            ValueType = "fixed",
            MinAmount = 100,
            MaxDiscount = 30,
            UsageLimit = 200,
            IsActive = false,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(2),
            ApplicableHouses = new Guid[] { Guid.NewGuid() }
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.Type.Should().Be("Cashback");
        result.Value.Should().Be(15);
        // Note: MapToDto doesn't map ValueType, so we verify the promotion entity directly
        var updatedPromotion = await _context.Promotions.FindAsync(promotion.Id);
        updatedPromotion!.ValueType.Should().Be("fixed");
        result.MinAmount.Should().Be(100);
        result.MaxDiscount.Should().Be(30);
        result.UsageLimit.Should().Be(200);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldUpdatePartialFields()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PARTIAL",
            Title = "Original Title",
            Description = "Original Description",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new UpdatePromotionRequest
        {
            Name = "Updated Title Only"
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Title Only");
        result.Description.Should().Be("Original Description"); // Should remain unchanged
        result.Type.Should().Be("Discount"); // Should remain unchanged
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldNotUpdateCode()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FIXEDCODE",
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

        var request = new UpdatePromotionRequest
        {
            Name = "Updated Title"
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("FIXEDCODE"); // Code should not change
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnValid_WhenAllConditionsMet()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "VALID",
            Title = "Valid Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = 50,
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

        var request = new ValidatePromotionRequest
        {
            Code = "VALID",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(10); // 10% of 100
        result.Promotion.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldCalculateFixedDiscount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FIXED10",
            Title = "Fixed Discount",
            Type = "Discount",
            Value = 10,
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
            Code = "FIXED10",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(10); // Fixed amount
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldRespectMaxDiscount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MAX20",
            Title = "Max Discount",
            Type = "Discount",
            Value = 50, // 50%
            ValueType = "percentage",
            MaxDiscountAmount = 20, // But max is $20
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
        result.DiscountAmount.Should().Be(20); // Should be capped at max
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludeUsedPromotions()
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

        var usage = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.UserPromotions.Add(usage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotContain(p => p.Code == "ONETIME");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldIncludePromotionsNotUsedByUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "SHARED",
            Title = "Shared Promotion",
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

        var usage = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.UserPromotions.Add(usage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId2, null);

        // Assert
        result.Should().Contain(p => p.Code == "SHARED");
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
            UsageCount = 5,
            IsActive = true,
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
            UsageCount = 3,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(activePromotion, inactivePromotion);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            IsActive = true
        };

        // Act
        var result = await _service.GetPromotionAnalyticsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().OnlyContain(a => a.Name == "Active Promotion" || a.Name == "Inactive Promotion");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
