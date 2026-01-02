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

public class PromotionServiceValidationTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceValidationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionNotFound()
    {
        // Arrange
        var request = new ValidatePromotionRequest
        {
            Code = "INVALID",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionNotStarted()
    {
        // Arrange
        var promotion = new Promotion
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
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "FUTURE",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not started");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionExpired()
    {
        // Arrange
        var promotion = new Promotion
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
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "EXPIRED",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenUsageLimitReached()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "LIMIT10",
            Title = "Limited Promotion",
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
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "LIMIT10",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("usage limit");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenHouseNotInApplicableHouses()
    {
        // Arrange
        var houseId = Guid.NewGuid();
        var otherHouseId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "HOUSE1",
            Title = "House Specific",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { houseId },
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
            Code = "HOUSE1",
            UserId = Guid.NewGuid(),
            Amount = 100,
            HouseId = otherHouseId
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not applicable");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnValid_WhenHouseInApplicableHouses()
    {
        // Arrange
        var houseId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "HOUSE1",
            Title = "House Specific",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { houseId },
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
            Code = "HOUSE1",
            UserId = Guid.NewGuid(),
            Amount = 100,
            HouseId = houseId
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenCodeIsEmpty()
    {
        // Arrange
        var request = new ValidatePromotionRequest
        {
            Code = "",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenCodeIsWhitespace()
    {
        // Arrange
        var request = new ValidatePromotionRequest
        {
            Code = "   ",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
