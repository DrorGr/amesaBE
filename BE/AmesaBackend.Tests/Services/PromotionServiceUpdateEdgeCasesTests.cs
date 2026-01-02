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

public class PromotionServiceUpdateEdgeCasesTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceUpdateEdgeCasesTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldNotUpdateName_WhenNameIsNull()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Original Title",
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
            Name = null
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Original Title");
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldNotUpdateName_WhenNameIsEmpty()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Original Title",
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
            Name = ""
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Original Title");
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldNotUpdateName_WhenNameIsSame()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Same Title",
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
            Name = "Same Title"
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Same Title");
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
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
            Description = "Updated Description"
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Updated Description");
        result.Name.Should().Be("Original Title"); // Should remain unchanged
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldHandleNullDescription()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Test",
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
            Description = null
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        // Service only updates when Description is not null, so original remains
        result.Description.Should().Be("Original Description");
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldHandleNullApplicableHouses()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Test",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { Guid.NewGuid() },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new UpdatePromotionRequest
        {
            ApplicableHouses = null
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        // Service only updates when ApplicableHouses is not null, so original array remains
        result.ApplicableHouses.Should().NotBeNull();
        result.ApplicableHouses!.Length.Should().Be(1);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldHandleEmptyApplicableHouses()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST",
            Title = "Test",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { Guid.NewGuid() },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new UpdatePromotionRequest
        {
            ApplicableHouses = Array.Empty<Guid>()
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.ApplicableHouses.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
