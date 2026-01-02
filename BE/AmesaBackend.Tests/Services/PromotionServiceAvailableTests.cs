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

public class PromotionServiceAvailableTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceAvailableTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldReturnActivePromotions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotion = new Promotion
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
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Code == "ACTIVE");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludeInactivePromotions()
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

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Code == "ACTIVE");
        result.Should().NotContain(p => p.Code == "INACTIVE");
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
        result.Should().NotBeNull();
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
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Code == "ACTIVE");
        result.Should().NotContain(p => p.Code == "FUTURE");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldFilterByHouseId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var houseId1 = Guid.NewGuid();
        var houseId2 = Guid.NewGuid();

        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "HOUSE1",
            Title = "House 1 Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { houseId1 },
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var promotion2 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "HOUSE2",
            Title = "House 2 Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { houseId2 },
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var generalPromotion = new Promotion
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

        _context.Promotions.AddRange(promotion1, promotion2, generalPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, houseId1);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Code == "HOUSE1");
        result.Should().Contain(p => p.Code == "GENERAL");
        result.Should().NotContain(p => p.Code == "HOUSE2");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludePromotionsAtUsageLimit()
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
            UsageLimit = 10,
            UsageCount = 5,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var limitReachedPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "LIMIT",
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

        _context.Promotions.AddRange(activePromotion, limitReachedPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Code == "ACTIVE");
        result.Should().NotContain(p => p.Code == "LIMIT");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldExcludeAlreadyUsedPromotions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "USED",
            Title = "Used Promotion",
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

        var userPromotion = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = promotion.Id,
            UsedAt = DateTime.UtcNow
        };
        _context.UserPromotions.Add(userPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain(p => p.Code == "USED");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldReturnEmptyList_WhenNoPromotionsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
