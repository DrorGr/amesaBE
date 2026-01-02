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

public class PromotionServiceCreateEdgeCasesTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceCreateEdgeCasesTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullCode()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = null!,
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act & Assert - Code.ToUpper() will throw an exception
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.CreatePromotionAsync(request, null));
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleEmptyCode()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act - Empty code should work (no duplicate check for empty)
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("");
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleWhitespaceCode()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "   ",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("   "); // Code is preserved as-is
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullDescription()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Description = null,
            Type = "Discount",
            Value = 10,
            ValueType = "percentage"
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullApplicableHouses()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.ApplicableHouses.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleEmptyApplicableHouses()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            ApplicableHouses = Array.Empty<Guid>()
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.ApplicableHouses.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullStartDate()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            StartDate = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().BeNull();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullEndDate()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            EndDate = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullUsageLimit()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            UsageLimit = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.UsageLimit.Should().BeNull();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullMinPurchaseAmount()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MinAmount = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.MinAmount.Should().BeNull();
    }

    [Fact]
    public async Task CreatePromotionAsync_ShouldHandleNullMaxDiscount()
    {
        // Arrange
        var request = new CreatePromotionRequest
        {
            Code = "TEST",
            Name = "Test Promotion",
            Type = "Discount",
            Value = 10,
            ValueType = "percentage",
            MaxDiscount = null
        };

        // Act
        var result = await _service.CreatePromotionAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.MaxDiscount.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
