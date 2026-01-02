using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Tests.TestHelpers;

namespace AmesaBackend.Tests.Services;

public class GamificationServiceGetUserAchievementsTests : IDisposable
{
    private readonly LotteryDbContext _context;
    private readonly Mock<ILogger<GamificationService>> _loggerMock;
    private readonly GamificationService _service;

    public GamificationServiceGetUserAchievementsTests()
    {
        _context = TestDbContextFactory.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GamificationService>>();
        _service = new GamificationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnEmptyList_WhenNoAchievements()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnAllAchievements_OrderedByUnlockedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievements = new List<UserAchievement>
        {
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "EntryBased",
                AchievementName = "First Entry",
                AchievementIcon = "🎟️",
                UnlockedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "WinBased",
                AchievementName = "Winner",
                AchievementIcon = "🏆",
                UnlockedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementType = "StreakBased",
                AchievementName = "On Fire",
                AchievementIcon = "🔥",
                UnlockedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.UserAchievements.AddRange(achievements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(a => a.UnlockedAt);
        result.First().Name.Should().Be("On Fire"); // Most recent
        result.Last().Name.Should().Be("First Entry"); // Oldest
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldOnlyReturnAchievementsForUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var achievement1 = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            AchievementType = "EntryBased",
            AchievementName = "User1 Achievement",
            AchievementIcon = "🎟️",
            UnlockedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var achievement2 = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            AchievementType = "EntryBased",
            AchievementName = "User2 Achievement",
            AchievementIcon = "🎟️",
            UnlockedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserAchievements.AddRange(achievement1, achievement2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("User1 Achievement");
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldMapAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = "EntryBased",
            AchievementName = "Test Achievement",
            AchievementIcon = "🎟️",
            UnlockedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserAchievements.Add(achievement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAchievementsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(achievement.Id.ToString());
        dto.Name.Should().Be("Test Achievement");
        dto.Description.Should().Be("Unlocked Test Achievement achievement");
        dto.Icon.Should().Be("🎟️");
        dto.UnlockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dto.Category.Should().Be("EntryBased");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
