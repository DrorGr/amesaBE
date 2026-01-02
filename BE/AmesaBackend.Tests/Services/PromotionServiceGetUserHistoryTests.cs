using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using AmesaBackend.Shared.Events;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class PromotionServiceGetUserHistoryTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceGetUserHistoryTests()
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
    public async Task GetUserPromotionHistoryAsync_ShouldReturnUserHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
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

        var userPromotion = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotion.Id,
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow
        };
        _context.UserPromotions.Add(userPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PromotionId.Should().Be(promotion.Id);
    }

    [Fact]
    public async Task GetUserPromotionHistoryAsync_ShouldReturnMultiplePromotions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST1",
            Title = "Test Promotion 1",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promotion2 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST2",
            Title = "Test Promotion 2",
            Type = "Discount",
            Value = 20,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(promotion1, promotion2);

        var userPromotion1 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotion1.Id,
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow.AddDays(-2)
        };

        var userPromotion2 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotion2.Id,
            DiscountAmount = 20,
            UsedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.UserPromotions.AddRange(userPromotion1, userPromotion2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserPromotionHistoryAsync_ShouldOnlyReturnUserPromotions()
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
            IsActive = true,
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
            UsedAt = DateTime.UtcNow
        };

        var userPromotion2 = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            PromotionId = promotion.Id,
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow
        };

        _context.UserPromotions.AddRange(userPromotion1, userPromotion2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PromotionId.Should().Be(promotion.Id);
    }

    [Fact]
    public async Task GetUserPromotionHistoryAsync_ShouldHandleNullPromotion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userPromotion = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = Guid.NewGuid(), // Non-existent promotion
            DiscountAmount = 10,
            UsedAt = DateTime.UtcNow
        };
        _context.UserPromotions.Add(userPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPromotionHistoryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        // Should handle gracefully - either return empty or skip null promotions
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
