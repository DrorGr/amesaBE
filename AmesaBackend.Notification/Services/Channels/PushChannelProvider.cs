using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Shared.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AmesaBackend.Notification.Services.Channels
{
    public class PushChannelProvider : IChannelProvider
    {
        public string ChannelName => NotificationChannelConstants.Push;

        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushChannelProvider> _logger;
        private readonly NotificationDbContext _context;
        private readonly IHttpRequest _httpRequest;

        private readonly string? _androidPlatformArn;
        private readonly string? _iosPlatformArn;

        public PushChannelProvider(
            IAmazonSimpleNotificationService snsClient,
            IConfiguration configuration,
            ILogger<PushChannelProvider> logger,
            NotificationDbContext context,
            IHttpRequest httpRequest)
        {
            _snsClient = snsClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _httpRequest = httpRequest;

            var pushConfig = _configuration.GetSection("NotificationChannels:Push:PlatformApplications");
            _androidPlatformArn = pushConfig["Android"];
            _iosPlatformArn = pushConfig["iOS"];
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                // Fetch user device tokens from Auth service or database
                var userData = await _httpRequest.GetRequest<Dictionary<string, object>>(
                    $"{_configuration["Services:AuthService:Url"]}/api/v1/users/{request.UserId}",
                    _configuration["JwtSettings:SecretKey"] ?? "");

                if (userData == null)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User data not found"
                    };
                }

                // TODO: Get device tokens from user profile or device registration table
                // For now, return not implemented
                _logger.LogInformation("Push channel provider - Device token management not yet implemented");
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Push channel requires device token registration"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification for user {UserId}", request.UserId);
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences)
        {
            // Push channel requires device registration
            return Task.FromResult(true);
        }

        public bool IsChannelEnabled(Guid userId)
        {
            // Check user channel preferences
            var preference = _context.UserChannelPreferences
                .FirstOrDefault(p => p.UserId == userId && p.Channel == NotificationChannelConstants.Push);
            
            return preference == null || preference.Enabled;
        }
    }
}

