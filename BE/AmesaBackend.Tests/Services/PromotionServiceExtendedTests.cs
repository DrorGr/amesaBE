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

public class PromotionServiceExtendedTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceExtendedTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
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

        // Act
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            IsActive = true
        });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByType()
    {
        // Arrange
        var discountPromotion = new Promotion
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
        var cashbackPromotion = new Promotion
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
        _context.Promotions.AddRange(discountPromotion, cashbackPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Type = "Discount"
        });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Type.Should().Be("Discount");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldSearchByTitle()
    {
        // Arrange
        var promotion1 = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PROMO1",
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
            Code = "PROMO2",
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

        // Act
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Search = "Summer"
        });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Summer Sale");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldSearchByCode()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "SUMMER2024",
            Title = "Summer Sale",
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
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            Search = "SUMMER"
        });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Be("SUMMER2024");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByStartDate()
    {
        // Arrange
        var pastPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "PAST",
            Title = "Past Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-10),
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
            StartDate = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.AddRange(pastPromotion, futurePromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            StartDate = DateTime.UtcNow
        });

        // Assert
        // The filter checks if StartDate is null or <= searchParams.StartDate
        // So both should be included if they meet the criteria
        result.Items.Should().Contain(p => p.Code == "PAST");
        // FUTURE promotion has StartDate in the future, so it won't be included when filtering by StartDate <= now
        result.Items.Should().NotContain(p => p.Code == "FUTURE");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldFilterByEndDate()
    {
        // Arrange
        var expiredPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "EXPIRED",
            Title = "Expired Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            EndDate = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var activePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "ACTIVE",
            Title = "Active Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            EndDate = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.AddRange(expiredPromotion, activePromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 1,
            Limit = 10,
            EndDate = DateTime.UtcNow
        });

        // Assert
        // The filter checks if EndDate is null or >= searchParams.EndDate
        // ACTIVE promotion has EndDate in the future, so it will be included
        result.Items.Should().Contain(p => p.Code == "ACTIVE");
        // EXPIRED promotion has EndDate in the past, so it won't be included when filtering by EndDate >= now
        result.Items.Should().NotContain(p => p.Code == "EXPIRED");
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldHandlePagination()
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

        // Act
        var result = await _service.GetPromotionsAsync(new PromotionSearchParams
        {
            Page = 2,
            Limit = 10
        });

        // Assert
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.Total.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldReturnNull_WhenCodeIsEmpty()
    {
        // Act
        var result = await _service.GetPromotionByCodeAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldReturnNull_WhenCodeIsWhitespace()
    {
        // Act
        var result = await _service.GetPromotionByCodeAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
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
        var result = await _service.GetPromotionByCodeAsync("test10");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("TEST10");
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldThrowException_WhenPromotionNotFound()
    {
        // Arrange
        var request = new UpdatePromotionRequest
        {
            Name = "Updated Promotion"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdatePromotionAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "TEST10",
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
            Name = "Updated Title"
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotion.Id, request);

        // Assert
        result.Name.Should().Be("Updated Title");
        var updated = await _context.Promotions.FindAsync(promotion.Id);
        updated!.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Original Description"); // Should remain unchanged
    }

    [Fact]
    public async Task DeletePromotionAsync_ShouldReturnFalse_WhenPromotionNotFound()
    {
        // Act
        var result = await _service.DeletePromotionAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePromotionAsync_ShouldDeletePromotion()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
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
        var result = await _service.DeletePromotionAsync(promotion.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Promotions.FindAsync(promotion.Id);
        deleted.Should().NotBeNull(); // Soft delete - promotion still exists
        deleted!.IsActive.Should().BeFalse(); // But IsActive is set to false
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
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionIsInactive()
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
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionHasExpired()
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
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenAmountBelowMinimum()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MIN100",
            Title = "Min Amount Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinPurchaseAmount = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "MIN100",
            UserId = Guid.NewGuid(),
            Amount = 50
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not met");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenUsageLimitExceeded()
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
        result.Message.Should().Contain("limit");
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
            UsageLimit = 100,
            UsageCount = 5,
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
            Code = "VALID",
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
    public async Task GetAvailablePromotionsAsync_ShouldReturnOnlyActivePromotions()
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.AddRange(activePromotion, inactivePromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(Guid.NewGuid(), null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldFilterByHouseId_WhenProvided()
    {
        // Arrange
        var houseId = Guid.NewGuid();
        var applicablePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "HOUSE1",
            Title = "House Specific",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = new Guid[] { houseId },
            IsActive = true,
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.AddRange(applicablePromotion, generalPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(Guid.NewGuid(), houseId);

        // Assert
        result.Should().Contain(p => p.Code == "HOUSE1");
        result.Should().Contain(p => p.Code == "GENERAL");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
