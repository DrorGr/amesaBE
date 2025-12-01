extern alias AuthApp;
using Xunit;
using Moq;
using FluentAssertions;
using AmesaBackend.Notification.Services;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AuthApp::AmesaBackend.Auth.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Tests.Services
{
    public class NotificationOrchestratorTests
    {
        private readonly Mock<ICache> _mockCache;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly Mock<IHttpRequest> _mockHttpRequest;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<NotificationOrchestrator>> _mockLogger;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly NotificationDbContext _context;
        private readonly NotificationOrchestrator _orchestrator;

        public NotificationOrchestratorTests()
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NotificationDbContext(options);
            _mockCache = new Mock<ICache>();
            _mockEventPublisher = new Mock<IEventPublisher>();
            _mockHttpRequest = new Mock<IHttpRequest>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<NotificationOrchestrator>>();
            _mockRateLimitService = new Mock<IRateLimitService>();

            var mockChannelProviders = new List<IChannelProvider>();

            _orchestrator = new NotificationOrchestrator(
                _context,
                mockChannelProviders,
                null!, // TemplateEngine - can be mocked if needed
                _mockCache.Object,
                _mockEventPublisher.Object,
                _mockHttpRequest.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockRateLimitService.Object
            );
        }

        [Fact]
        public async Task SendMultiChannelAsync_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new NotificationRequest
            {
                UserId = userId,
                Type = "test",
                Title = "Test Notification",
                Message = "Test message",
                Language = "en"
            };
            var channels = new List<string> { "email" };

            _mockCache.Setup(c => c.GetRecordAsync<Dictionary<string, object>>(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((Dictionary<string, object>?)null);

            // Act
            var result = await _orchestrator.SendMultiChannelAsync(userId, request, channels);

            // Assert
            result.Should().NotBeNull();
            result.NotificationId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task GetDeliveryStatusAsync_WithValidNotificationId_ReturnsStatuses()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var delivery = new NotificationDelivery
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                Channel = "email",
                Status = "sent",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationDeliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orchestrator.GetDeliveryStatusAsync(notificationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Channel.Should().Be("email");
            result[0].Status.Should().Be("sent");
        }

        [Fact]
        public async Task GetDeliveryStatusAsync_WithInvalidNotificationId_ReturnsEmpty()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _orchestrator.GetDeliveryStatusAsync(invalidId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}

