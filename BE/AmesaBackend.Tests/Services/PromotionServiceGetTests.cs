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

public class PromotionServiceGetTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceGetTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPromotionByIdAsync_ShouldReturnPromotion_WhenFound()
    {
        // Arrange
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
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionByIdAsync(promotionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(promotionId);
        result.Code.Should().Be("TEST10");
    }

    [Fact]
    public async Task GetPromotionByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var promotionId = Guid.NewGuid();

        // Act
        var result = await _service.GetPromotionByIdAsync(promotionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldReturnPromotion_WhenFound()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "CODE10",
            Title = "Code Promotion",
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
        var result = await _service.GetPromotionByCodeAsync("CODE10");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("CODE10");
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var code = "NONEXISTENT";

        // Act
        var result = await _service.GetPromotionByCodeAsync(code);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldHandleCaseInsensitive()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "lowercase",
            Title = "Lowercase Code",
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
        var result = await _service.GetPromotionByCodeAsync("LOWERCASE");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("lowercase");
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldHandleNullCode()
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

        // Act
        var result = await _service.GetPromotionByCodeAsync("ANYCODE");

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
