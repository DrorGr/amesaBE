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

public class PromotionServiceApplyTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PromotionService>> _loggerMock;
    private readonly PromotionService _service;

    public PromotionServiceApplyTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PromotionService>>();
        _service = new PromotionService(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [Fact(Skip = "Requires relational database for ExecuteSqlRawAsync row locking. In-memory database doesn't support SQL-specific features like FOR UPDATE.")]
    public async Task ApplyPromotionAsync_ShouldThrowException_WhenPromotionNotFound()
    {
        // Arrange
        var request = new ApplyPromotionRequest
        {
            Code = "INVALID",
            UserId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            Amount = 100,
            DiscountAmount = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ApplyPromotionAsync(request));
    }

    // Note: ApplyPromotionAsync tests are skipped because the method uses ExecuteSqlRawAsync
    // with FOR UPDATE which requires a relational database. In-memory database doesn't support
    // SQL-specific features like row locking. These tests would need a real PostgreSQL database.

    [Fact(Skip = "Requires relational database for ExecuteSqlRawAsync row locking. In-memory database doesn't support SQL-specific features like FOR UPDATE.")]
    public async Task ApplyPromotionAsync_ShouldThrowException_WhenDiscountAmountMismatch()
    {
        // Arrange
        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = "MISMATCH",
            Title = "Mismatch Test",
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

        var request = new ApplyPromotionRequest
        {
            Code = "MISMATCH",
            UserId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            Amount = 100,
            DiscountAmount = 50 // Should be 10 (10% of 100), but we're passing 50
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApplyPromotionAsync(request));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
