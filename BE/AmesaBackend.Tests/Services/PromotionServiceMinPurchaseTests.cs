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

public class PromotionServiceMinPurchaseTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceMinPurchaseTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenMinPurchaseNotMet()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MIN100",
            Title = "Min Purchase Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = 100,
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
            Code = "MIN100",
            UserId = Guid.NewGuid(),
            Amount = 50 // Less than minimum
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Minimum purchase");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnValid_WhenMinPurchaseMet()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MIN100",
            Title = "Min Purchase Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = 100,
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
            Code = "MIN100",
            UserId = Guid.NewGuid(),
            Amount = 150 // Meets minimum
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnValid_WhenMinPurchaseExactlyMet()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MIN100",
            Title = "Min Purchase Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = 100,
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
            Code = "MIN100",
            UserId = Guid.NewGuid(),
            Amount = 100 // Exactly minimum
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnValid_WhenNoMinPurchase()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NOMIN",
            Title = "No Min Purchase",
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
            Amount = 10 // Small amount, but no minimum
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionInactive()
    {
        // Arrange
        var promotion = new Promotion
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
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "INACTIVE",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not active");
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
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            UsageLimit = 100,
            UsageCount = 10,
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
        result.DiscountAmount.Should().BeGreaterThan(0);
        result.Promotion.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
