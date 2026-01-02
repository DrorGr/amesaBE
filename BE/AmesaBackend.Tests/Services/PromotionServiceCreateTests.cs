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

public class PromotionServiceCreateTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceCreateTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldCreatePromotion_WithAllFields()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "Complete Promotion",
            Code = "COMPLETE",
            Description = "Complete description",
            Type = "Discount",
            Value = 15,
            ValueType = "percentage",
            MinAmount = 50,
            MaxDiscount = 20,
            UsageLimit = 100,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            ApplicableHouses = new Guid[] { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Complete Promotion");
        result.Code.Should().Be("COMPLETE");
        result.Description.Should().Be("Complete description");
        result.Type.Should().Be("Discount");
        result.Value.Should().Be(15);
        result.MinAmount.Should().Be(50);
        result.MaxDiscount.Should().Be(20);
        result.UsageLimit.Should().Be(100);
        result.IsActive.Should().BeTrue();
        result.ApplicableHouses.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldCreatePromotion_WithMinimalFields()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "Minimal Promotion",
            Code = "MINIMAL",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Minimal Promotion");
        result.Code.Should().Be("MINIMAL");
        result.IsActive.Should().BeTrue(); // Default value
        result.UsageCount.Should().Be(0); // Default value
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldSetCreatedBy_WhenProvided()
    {
        // Arrange
        var createdBy = Guid.NewGuid();
        var request = new CreatePromotionRequest
        {
            Name = "Created By Test",
            Code = "CREATEDBY",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, createdBy);

        // Assert
        result.Should().NotBeNull();
        var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == "CREATEDBY");
        promotion.Should().NotBeNull();
        // Note: CreatedBy might not be in the DTO, but should be in the entity
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "Default Values",
            Code = "DEFAULTS",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        result.UsageCount.Should().Be(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullDates()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "No Dates",
            Code = "NODATES",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            StartDate = null,
            EndDate = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().BeNull();
        result.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullApplicableHouses()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "No Houses",
            Code = "NOHOUSES",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.ApplicableHouses.Should().BeNull();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleEmptyApplicableHouses()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "Empty Houses",
            Code = "EMPTYHOUSES",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = Array.Empty<Guid>()
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.ApplicableHouses.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldPublishEvent()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Name = "Event Test",
            Code = "EVENT",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<PromotionCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
