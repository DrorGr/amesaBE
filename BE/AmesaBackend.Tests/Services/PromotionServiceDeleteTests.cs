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

public class PromotionServiceDeleteTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceDeleteTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DeletePromotionAsync_ShouldReturnFalse_WhenPromotionNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeletePromotionAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePromotionAsync_ShouldSoftDelete_WhenPromotionExists()
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

        // Act
        var result = await _service.DeletePromotionAsync(promotion.Id);

        // Assert
        result.Should().BeTrue();
        var deletedPromotion = await _context.Promotions.FindAsync(promotion.Id);
        deletedPromotion.Should().NotBeNull();
        deletedPromotion!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePromotionAsync_ShouldReturnTrue_WhenPromotionAlreadyDeleted()
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
            IsActive = false, // Already deleted
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeletePromotionAsync(promotion.Id);

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
