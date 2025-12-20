using Xunit;
using FluentAssertions;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Controllers;
using AmesaBackend.Shared.Contracts;
using AmesaBackend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AmesaBackend.Tests.Integration
{
    /// <summary>
    /// Integration tests for GamificationController
    /// Tests the full HTTP request/response cycle
    /// </summary>
    public class GamificationControllerIntegrationTests : IDisposable
    {
        private readonly LotteryDbContext _context;
        private readonly GamificationService _gamificationService;
        private readonly GamificationController _controller;
        private readonly Guid _testUserId;
        private readonly MockHttpContext _mockHttpContext;

        public GamificationControllerIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<LotteryDbContext>()
                .UseInMemoryDatabase(databaseName: $"GamificationIntegrationTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new LotteryDbContext(options);
            var logger = new Mock<ILogger<GamificationService>>().Object;
            _gamificationService = new GamificationService(_context, logger);
            
            var controllerLogger = new Mock<ILogger<GamificationController>>().Object;
            _controller = new GamificationController(_gamificationService, controllerLogger);
            
            _testUserId = Guid.NewGuid();
            _mockHttpContext = new MockHttpContext(_testUserId);
            _controller.ControllerContext.HttpContext = _mockHttpContext.HttpContext;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetUserGamification Tests

        [Fact]
        public async Task GetUserGamification_NoRecord_ReturnsDefaultValues()
        {
            // Act
            var result = await _controller.GetUserGamification();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<UserGamificationDto>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.TotalPoints.Should().Be(0);
            response.Data.CurrentLevel.Should().Be(1);
            response.Data.CurrentTier.Should().Be("Bronze");
            response.Data.CurrentStreak.Should().Be(0);
            response.Data.RecentAchievements.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserGamification_WithRecord_ReturnsGamificationData()
        {
            // Arrange
            var gamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 1000,
                CurrentLevel = 3,
                CurrentTier = "Silver",
                CurrentStreak = 5,
                LongestStreak = 10,
                LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(gamification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUserGamification();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<UserGamificationDto>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.TotalPoints.Should().Be(1000);
            response.Data.CurrentLevel.Should().Be(3);
            response.Data.CurrentTier.Should().Be("Silver");
            response.Data.CurrentStreak.Should().Be(5);
            response.Data.LongestStreak.Should().Be(10);
        }

        [Fact]
        public async Task GetUserGamification_WithAchievements_ReturnsRecentAchievements()
        {
            // Arrange
            var gamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 500,
                CurrentLevel = 2,
                CurrentTier = "Bronze",
                CurrentStreak = 0,
                LongestStreak = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(gamification);

            for (int i = 1; i <= 15; i++)
            {
                _context.UserAchievements.Add(new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "EntryBased",
                    AchievementName = $"Achievement {i}",
                    AchievementIcon = "🎟️",
                    UnlockedAt = DateTime.UtcNow.AddDays(-i),
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUserGamification();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<UserGamificationDto>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.RecentAchievements.Should().HaveCount(10); // Limited to 10 most recent
            response.Data.RecentAchievements.Should().BeInDescendingOrder(a => a.UnlockedAt);
        }

        [Fact]
        public async Task GetUserGamification_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var invalidContext = new MockHttpContext(Guid.Empty, hasValidClaim: false);
            _controller.ControllerContext.HttpContext = invalidContext.HttpContext;

            // Act
            var result = await _controller.GetUserGamification();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            var response = unauthorizedResult?.Value as StandardApiResponse<UserGamificationDto>;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Code.Should().Be("AUTHENTICATION_ERROR");
        }

        #endregion

        #region GetUserAchievements Tests

        [Fact]
        public async Task GetUserAchievements_NoAchievements_ReturnsEmptyList()
        {
            // Act
            var result = await _controller.GetUserAchievements();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<List<AchievementDto>>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserAchievements_WithAchievements_ReturnsAllAchievements()
        {
            // Arrange
            var achievements = new List<UserAchievement>
            {
                new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "EntryBased",
                    AchievementName = "First Entry",
                    AchievementIcon = "🎟️",
                    UnlockedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "WinBased",
                    AchievementName = "Winner",
                    AchievementIcon = "🏆",
                    UnlockedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new UserAchievement
                {
                    Id = Guid.NewGuid(),
                    UserId = _testUserId,
                    AchievementType = "StreakBased",
                    AchievementName = "On Fire",
                    AchievementIcon = "🔥",
                    UnlockedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            _context.UserAchievements.AddRange(achievements);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUserAchievements();
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<List<AchievementDto>>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().HaveCount(3);
            response.Data.Should().BeInDescendingOrder(a => a.UnlockedAt);
            response.Data.Should().Contain(a => a.Name == "First Entry");
            response.Data.Should().Contain(a => a.Name == "Winner");
            response.Data.Should().Contain(a => a.Name == "On Fire");
        }

        [Fact]
        public async Task GetUserAchievements_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var invalidContext = new MockHttpContext(Guid.Empty, hasValidClaim: false);
            _controller.ControllerContext.HttpContext = invalidContext.HttpContext;

            // Act
            var result = await _controller.GetUserAchievements();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            var response = unauthorizedResult?.Value as StandardApiResponse<List<AchievementDto>>;

            // Assert
            unauthorizedResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Error.Should().NotBeNull();
            response.Error!.Code.Should().Be("AUTHENTICATION_ERROR");
        }

        #endregion

        #region AwardPoints Tests

        [Fact]
        public async Task AwardPoints_ValidRequest_AwardsPoints()
        {
            // Arrange
            var request = new GamificationController.AwardPointsRequest
            {
                UserId = _testUserId,
                Points = 50,
                Reason = "Test Points",
                ReferenceId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.AwardPoints(request);
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<object>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();

            // Verify points were awarded
            var gamification = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            gamification.Should().NotBeNull();
            gamification!.TotalPoints.Should().Be(50);

            // Verify history entry
            var history = await _context.PointsHistory
                .FirstOrDefaultAsync(h => h.UserId == _testUserId);
            history.Should().NotBeNull();
            history!.PointsChange.Should().Be(50);
            history.Reason.Should().Be("Test Points");
            history.ReferenceId.Should().Be(request.ReferenceId);
        }

        [Fact]
        public async Task AwardPoints_ExistingUser_UpdatesPoints()
        {
            // Arrange
            var existingGamification = new UserGamification
            {
                UserId = _testUserId,
                TotalPoints = 100,
                CurrentLevel = 1,
                CurrentTier = "Bronze",
                CurrentStreak = 0,
                LongestStreak = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserGamification.Add(existingGamification);
            await _context.SaveChangesAsync();

            var request = new GamificationController.AwardPointsRequest
            {
                UserId = _testUserId,
                Points = 50,
                Reason = "Additional Points"
            };

            // Act
            var result = await _controller.AwardPoints(request);
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<object>;

            // Assert
            okResult.Should().NotBeNull();
            response!.Success.Should().BeTrue();

            var updated = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            updated!.TotalPoints.Should().Be(150);
        }

        [Fact]
        public async Task AwardPoints_ZeroPoints_DoesNothing()
        {
            // Arrange
            var request = new GamificationController.AwardPointsRequest
            {
                UserId = _testUserId,
                Points = 0,
                Reason = "Zero Points"
            };

            // Act
            var result = await _controller.AwardPoints(request);
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<object>;

            // Assert
            okResult.Should().NotBeNull();
            response!.Success.Should().BeTrue();

            // Verify no gamification record was created
            var gamification = await _context.UserGamification
                .FirstOrDefaultAsync(g => g.UserId == _testUserId);
            gamification.Should().BeNull();
        }

        #endregion

        #region CheckAchievements Tests

        [Fact]
        public async Task CheckAchievements_FirstEntry_UnlocksFirstEntryAchievement()
        {
            // Arrange
            var ticket = new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                HouseId = Guid.NewGuid(),
                TicketNumber = "T001",
                Status = "active",
                PurchasePrice = 50,
                PurchaseDate = DateTime.UtcNow,
                IsWinner = false
            };
            _context.LotteryTickets.Add(ticket);
            await _context.SaveChangesAsync();

            var request = new GamificationController.CheckAchievementsRequest
            {
                UserId = _testUserId,
                ActionType = "EntryPurchase"
            };

            // Act
            var result = await _controller.CheckAchievements(request);
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<List<AchievementDto>>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().Contain(a => a.Name == "First Entry");

            // Verify achievement was saved
            var achievement = await _context.UserAchievements
                .FirstOrDefaultAsync(a => a.UserId == _testUserId && a.AchievementName == "First Entry");
            achievement.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckAchievements_FirstWin_UnlocksWinnerAchievement()
        {
            // Arrange
            var ticket = new LotteryTicket
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                HouseId = Guid.NewGuid(),
                TicketNumber = "T001",
                Status = "active",
                PurchasePrice = 50,
                PurchaseDate = DateTime.UtcNow,
                IsWinner = true
            };
            _context.LotteryTickets.Add(ticket);
            await _context.SaveChangesAsync();

            var request = new GamificationController.CheckAchievementsRequest
            {
                UserId = _testUserId,
                ActionType = "Win"
            };

            // Act
            var result = await _controller.CheckAchievements(request);
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<List<AchievementDto>>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().Contain(a => a.Name == "Winner");
        }

        [Fact]
        public async Task CheckAchievements_NoNewAchievements_ReturnsEmptyList()
        {
            // Arrange
            // Add existing achievement
            var existingAchievement = new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                AchievementType = "EntryBased",
                AchievementName = "First Entry",
                AchievementIcon = "🎟️",
                UnlockedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _context.UserAchievements.Add(existingAchievement);
            await _context.SaveChangesAsync();

            var request = new GamificationController.CheckAchievementsRequest
            {
                UserId = _testUserId,
                ActionType = "EntryPurchase"
            };

            // Act
            var result = await _controller.CheckAchievements(request);
            var okResult = result.Result as OkObjectResult;
            var response = okResult?.Value as StandardApiResponse<List<AchievementDto>>;

            // Assert
            okResult.Should().NotBeNull();
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().BeEmpty(); // No new achievements
        }

        #endregion
    }

    /// <summary>
    /// Helper class to mock HTTP context with user claims
    /// </summary>
    internal class MockHttpContext
    {
        public HttpContext HttpContext { get; }

        public MockHttpContext(Guid userId, bool hasValidClaim = true)
        {
            var context = new DefaultHttpContext();
            
            if (hasValidClaim)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                };
                var identity = new ClaimsIdentity(claims, "Test");
                context.User = new ClaimsPrincipal(identity);
            }
            else
            {
                context.User = new ClaimsPrincipal();
            }

            HttpContext = context;
        }
    }
}

