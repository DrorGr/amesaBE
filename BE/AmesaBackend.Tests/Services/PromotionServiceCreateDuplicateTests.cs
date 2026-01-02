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

public class PromotionServiceCreateDuplicateTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceCreateDuplicateTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldThrowException_WhenCodeExists()
    {
        // Arrange
        var existingPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "EXISTING",
            Title = "Existing Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(existingPromotion);
        await _context.SaveChangesAsync();

        var request = new CreatePromotionRequest
        {
            Name = "New Promotion",
            Code = "EXISTING", // Same code
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePromotionAsync(request, null));
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldThrowException_WhenCodeExistsCaseInsensitive()
    {
        // Arrange
        var existingPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "existing",
            Title = "Existing Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(existingPromotion);
        await _context.SaveChangesAsync();

        var request = new CreatePromotionRequest
        {
            Name = "New Promotion",
            Code = "EXISTING", // Different case
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePromotionAsync(request, null));
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldAllowDifferentCodes()
    {
        // Arrange
        var existingPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "CODE1",
            Title = "Promotion 1",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(existingPromotion);
        await _context.SaveChangesAsync();

        var request = new CreatePromotionRequest
        {
            Name = "New Promotion",
            Code = "CODE2", // Different code
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("CODE2");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
