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

public class PromotionServiceCalculateDiscountTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceCalculateDiscountTests()
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
            Title = "10% Off",
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
    public async Task ValidatePromotionAsync_ShouldCalculatePercentageDiscount_WithMaxDiscount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PERCENT10MAX5",
            Title = "10% Off Max $5",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MaxDiscountAmount = 5,
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
            Code = "PERCENT10MAX5",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(5); // 10% of 100 = 10, but max is 5
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldCalculateFixedDiscount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FIXED20",
            Title = "$20 Off",
            Type = "Discount",
            Value = 20,
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
            Code = "FIXED20",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(20);
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldCalculateFixedDiscount_WhenValueTypeIsFixedAmount()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FIXEDAMOUNT",
            Title = "$15 Off",
            Type = "Discount",
            Value = 15,
            ValueType = "fixed_amount",
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
            Code = "FIXEDAMOUNT",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(15);
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
            Amount = 50 // Less than discount
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(50); // Should not exceed purchase amount
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnZeroDiscount_WhenMinPurchaseNotMet()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MIN100",
            Title = "Min $100",
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
    public async Task ValidatePromotionAsync_ShouldHandleFreeTickets_ValueType()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "FREETICKETS",
            Title = "Free Tickets",
            Type = "Discount",
            Value = 2,
            ValueType = "free_tickets",
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
            Code = "FREETICKETS",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(0); // Free tickets don't provide discount
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldDefaultToFixed_WhenValueTypeIsNull()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "DEFAULT",
            Title = "Default",
            Type = "Discount",
            Value = 25,
            ValueType = null,
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
            Code = "DEFAULT",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(25); // Defaults to fixed amount
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
