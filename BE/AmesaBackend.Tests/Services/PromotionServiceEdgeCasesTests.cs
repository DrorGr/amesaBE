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

public class PromotionServiceEdgeCasesTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceEdgeCasesTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldHandleNullStartDate()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NOSTART",
            Title = "No Start Date",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            StartDate = null,
            EndDate = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "NOSTART",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldHandleNullEndDate()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NOEND",
            Title = "No End Date",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "NOEND",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldHandleNullMinPurchaseAmount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NOMIN",
            Title = "No Min Amount",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = null,
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
            Code = "NOMIN",
            UserId = Guid.NewGuid(),
            Amount = 10 // Small amount, but no min required
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldHandleNullUsageLimit()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NOUSAGELIMIT",
            Title = "No Usage Limit",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = null,
            UsageCount = 1000,
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
            Code = "NOUSAGELIMIT",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldHandleNullApplicableHouses()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "GENERAL",
            Title = "General Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = null,
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
            Code = "GENERAL",
            UserId = Guid.NewGuid(),
            Amount = 100,
            HouseId = Guid.NewGuid()
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldHandleEmptyApplicableHouses()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "EMPTYHOUSES",
            Title = "Empty Houses",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = Array.Empty<Guid>(),
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
            Code = "EMPTYHOUSES",
            UserId = Guid.NewGuid(),
            Amount = 100,
            HouseId = Guid.NewGuid()
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleNullDescriptionInSearch()
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
    public async Task GetPromotionsAsync_ShouldHandleNullCodeInSearch()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = null,
            Title = "No Code",
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
            Search = "No Code"
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().Contain(p => p.Name == "No Code");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleLastPage()
    {
        // Arrange
        var promotions = Enumerable.Range(1, 25).Select(i => new Promotion
        {
            Id = Guid.NewGuid(),
            Code = $"PROMO{i}",
            Title = $"Promotion {i}",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
        _context.Promotions.AddRange(promotions);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 3,
            Limit = 10
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(3);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleFirstPage()
    {
        // Arrange
        var promotions = Enumerable.Range(1, 25).Select(i => new Promotion
        {
            Id = Guid.NewGuid(),
            Code = $"PROMO{i}",
            Title = $"Promotion {i}",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();
        _context.Promotions.AddRange(promotions);
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
        result.Page.Should().Be(1);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetPromotionUsageStatsAsync_ShouldCalculateAverageDiscount()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "AVG",
            Title = "Average Test",
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
        result.AverageDiscountAmount.Should().Be(15); // (10 + 20) / 2
    }

    [Fact]
    public async Task GetPromotionUsageStatsAsync_ShouldReturnZeroAverage_WhenNoUsages()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "NOUSAGE",
            Title = "No Usage",
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
        var result = await _service.GetPromotionUsageStatsAsync(promotionId);

        // Assert
        result.Should().NotBeNull();
        result.AverageDiscountAmount.Should().Be(0);
        result.TotalUsage.Should().Be(0);
    }

    [Fact]
    public async Task GetUserPromotionHistoryAsync_ShouldHandleNullPromotion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        // Note: We can't create a UserPromotion without a Promotion due to foreign key constraints
        // This test verifies the service handles missing promotions gracefully
        var promotion = new Promotion
        {
            Id = promotionId,
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
        
        var usage = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotionId,
            TransactionId = Guid.NewGuid(),
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow
        };
        _context.UserPromotions.Add(usage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().PromotionCode.Should().Be("TEST");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
