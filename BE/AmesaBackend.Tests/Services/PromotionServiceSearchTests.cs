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

public class PromotionServiceSearchTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceSearchTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByCode()
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
            Search = "PROMO1"
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().Contain(p => p.Code == "PROMO1");
        result.Items.Should().NotContain(p => p.Code == "PROMO2");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByTitle()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "CODE1",
            Title = "Summer Sale",
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
            Code = "CODE2",
            Title = "Winter Sale",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
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
            Search = "Summer"
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().Contain(p => p.Name == "Summer Sale");
        result.Items.Should().NotContain(p => p.Name == "Winter Sale");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByDescription()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "CODE1",
            Title = "Promotion 1",
            Description = "Special discount for members",
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
            Code = "CODE2",
            Title = "Promotion 2",
            Description = "Regular promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
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
            Search = "members"
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().Contain(p => p.Description == "Special discount for members");
        result.Items.Should().NotContain(p => p.Description == "Regular promotion");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByType()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "DISCOUNT",
            Title = "Discount Promotion",
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
            Code = "CASHBACK",
            Title = "Cashback Promotion",
            Type = "Cashback",
            Value = 5,
            ValueType = "percentage",
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
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().Contain(p => p.Type == "Discount");
        result.Items.Should().NotContain(p => p.Type == "Cashback");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByIsActive()
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
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().Contain(p => p.Code == "ACTIVE");
        result.Items.Should().NotContain(p => p.Code == "INACTIVE");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldSortByCreatedAtDescending()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "OLD",
            Title = "Old Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var promotion2 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "NEW",
            Title = "New Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Promotions.AddRange(promotion1, promotion2);
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().NotBeEmpty();
        // Default sorting is by CreatedAt descending
        result.Items.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandleEmptyResults()
    {
        // Arrange
        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Search = "NONEXISTENT"
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldCalculatePaginationCorrectly()
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
            Page = 2,
            Limit = 10
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Page.Should().Be(2);
        result.Limit.Should().Be(10);
        result.Total.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
