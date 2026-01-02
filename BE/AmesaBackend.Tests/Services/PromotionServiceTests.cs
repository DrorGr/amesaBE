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

public class PromotionServiceTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldCreateNewPromotion()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST10",
            Name = "Test Promotion",
            Description = "Test description",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true
        };
        var createdBy = Guid.NewGuid();

        // Act
        var result = await _service.CreatePromotionAsync(request, createdBy);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("TEST10");
        result.Name.Should().Be("Test Promotion");
        
        var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == result.Id);
        promotion.Should().NotBeNull();
        
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<PromotionCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldThrowException_WhenCodeAlreadyExists()
    {
        // Arrange
        var existingPromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "EXISTING",
            Title = "Existing",
            Type = "Discount",
            Value = 5,
            ValueType = "percentage",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(existingPromotion);
        await _context.SaveChangesAsync();

        var request = new CreatePromotionRequest
        {
            Code = "existing", // Case-insensitive
            Name = "New Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePromotionAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPromotionByIdAsync_ShouldReturnPromotion_WhenExists()
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
    public async Task GetPromotionByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetPromotionByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_ShouldReturnPromotion_WhenExists()
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
        var result = await _service.GetPromotionByCodeAsync("test10"); // Case-insensitive

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("TEST10");
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
    public async Task UpdatePromotionAsync_ShouldUpdatePromotion()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "TEST10",
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

        var updateRequest = new UpdatePromotionRequest
        {
            Name = "Updated Title",
            Description = "Updated description",
            Value = 20
        };

        // Act
        var result = await _service.UpdatePromotionAsync(promotionId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Title");
        result.Value.Should().Be(20);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldThrowException_WhenPromotionNotFound()
    {
        // Arrange
        var updateRequest = new UpdatePromotionRequest
        {
            Name = "Updated Title"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdatePromotionAsync(Guid.NewGuid(), updateRequest));
    }

    [Fact]
    public async Task DeletePromotionAsync_ShouldSoftDeletePromotion()
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
        var result = await _service.DeletePromotionAsync(promotionId);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == promotionId);
        deleted!.IsActive.Should().BeFalse();
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
    public async Task ValidatePromotionAsync_ShouldReturnValid_WhenPromotionIsValid()
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
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "TEST10",
            UserId = Guid.NewGuid(),
            Amount = 100,
            HouseId = null
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Promotion.Should().NotBeNull();
        result.DiscountAmount.Should().BeGreaterThan(0);
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
        result.ErrorCode.Should().Be("PROMOTION_CODE_INVALID");
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
        result.ErrorCode.Should().Be("PROMOTION_NOT_FOUND");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenPromotionInactive()
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
        result.ErrorCode.Should().Be("PROMOTION_INACTIVE");
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
        result.ErrorCode.Should().Be("PROMOTION_EXPIRED");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenUsageLimitReached()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "LIMITED",
            Title = "Limited Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            UsageLimit = 5,
            UsageCount = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "LIMITED",
            UserId = Guid.NewGuid(),
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("PROMOTION_USAGE_LIMIT_REACHED");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenMinPurchaseAmountNotMet()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MIN100",
            Title = "Min Purchase Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            MinPurchaseAmount = 100,
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
        result.ErrorCode.Should().Be("PROMOTION_MIN_PURCHASE_NOT_MET");
    }

    [Fact]
    public async Task ValidatePromotionAsync_ShouldReturnInvalid_WhenAlreadyUsed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "ONETIME",
            Title = "One Time Promotion",
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
            PromotionId = promotionId,
            UsedAt = DateTime.UtcNow
        };
        _context.UserPromotions.Add(userPromotion);
        await _context.SaveChangesAsync();

        var request = new ValidatePromotionRequest
        {
            Code = "ONETIME",
            UserId = userId,
            Amount = 100
        };

        // Act
        var result = await _service.ValidatePromotionAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("PROMOTION_ALREADY_USED");
    }

    [Fact(Skip = "Requires relational database for ExecuteSqlRawAsync row locking. In-memory database doesn't support SQL-specific features like FOR UPDATE.")]
    public async Task ApplyPromotionAsync_ShouldApplyPromotionSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var promotion = new Promotion
        {
            Id = promotionId,
            Code = "APPLY10",
            Title = "Apply Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            UsageLimit = 100,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        var request = new ApplyPromotionRequest
        {
            Code = "APPLY10",
            UserId = userId,
            TransactionId = transactionId,
            Amount = 100,
            DiscountAmount = 10
        };

        // Act
        var result = await _service.ApplyPromotionAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.PromotionId.Should().Be(promotionId);
        result.UserId.Should().Be(userId);
        result.TransactionId.Should().Be(transactionId);
        result.DiscountAmount.Should().Be(10);
        
        var updatedPromotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == promotionId);
        updatedPromotion!.UsageCount.Should().Be(1);
        
        var userPromotion = await _context.UserPromotions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PromotionId == promotionId);
        userPromotion.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPromotionsAsync_ShouldReturnPagedResults()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Code = $"CODE{i}",
                Title = $"Promotion {i}",
                Type = "Discount",
                Value = 10,
                ValueType = "percentage",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Promotions.Add(promotion);
        }
        await _context.SaveChangesAsync();

        var searchParams = new PromotionSearchParams
        {
            Page = 1,
            Limit = 10
        };

        // Act
        var result = await _service.GetPromotionsAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.Total.Should().Be(15);
        result.Page.Should().Be(1);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
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
        _context.Promotions.Add(activePromotion);
        
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
        _context.Promotions.Add(inactivePromotion);
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
        result.Items.Should().HaveCount(1);
        result.Items[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailablePromotionsAsync_ShouldReturnOnlyAvailablePromotions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var availablePromotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "AVAILABLE",
            Title = "Available Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Promotions.Add(availablePromotion);
        
        var usedPromotion = new Promotion
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
        _context.Promotions.Add(usedPromotion);
        
        var userPromotion = new UserPromotion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PromotionId = usedPromotion.Id,
            UsedAt = DateTime.UtcNow
        };
        _context.UserPromotions.Add(userPromotion);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAvailablePromotionsAsync(userId, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Code.Should().Be("AVAILABLE");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
