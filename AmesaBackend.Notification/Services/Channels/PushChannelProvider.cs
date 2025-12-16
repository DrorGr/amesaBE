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
        private readonly IDeviceRegistrationService _deviceRegistrationService;

        private readonly string? _androidPlatformArn;
        private readonly string? _iosPlatformArn;

        public PushChannelProvider(
            IAmazonSimpleNotificationService snsClient,
            IConfiguration configuration,
            ILogger<PushChannelProvider> logger,
            NotificationDbContext context,
            IHttpRequest httpRequest,
            IDeviceRegistrationService deviceRegistrationService)
        {
            _snsClient = snsClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _httpRequest = httpRequest;
            _deviceRegistrationService = deviceRegistrationService;

            var pushConfig = _configuration.GetSection("NotificationChannels:Push:PlatformApplications");
            _androidPlatformArn = pushConfig["Android"];
            _iosPlatformArn = pushConfig["iOS"];
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                // Get device tokens from device registration table
                var deviceTokens = await _deviceRegistrationService.GetUserDeviceTokensAsync(request.UserId);

                if (deviceTokens == null || deviceTokens.Count == 0)
                {
                    _logger.LogDebug("No device tokens found for user {UserId}", request.UserId);
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "No registered devices found for user"
                    };
                }

                var successCount = 0;
                var failureCount = 0;
                var errors = new List<string>();

                // Send push notification to each device token
                foreach (var deviceToken in deviceTokens)
                {
                    try
                    {
                        // Determine platform from device registration
                        var device = await _context.DeviceRegistrations
                            .FirstOrDefaultAsync(d => d.UserId == request.UserId && d.DeviceToken == deviceToken);

                        if (device == null || !device.IsActive)
                        {
                            continue;
                        }

                        // Create SNS endpoint if needed and send notification
                        // This is a simplified implementation - in production you'd:
                        // 1. Create/update SNS platform endpoint
                        // 2. Send notification via SNS
                        // 3. Handle platform-specific formatting

                        var platformArn = device.Platform.ToLower() switch
                        {
                            "ios" => _iosPlatformArn,
                            "android" => _androidPlatformArn,
                            _ => null
                        };

                        if (string.IsNullOrEmpty(platformArn))
                        {
                            _logger.LogWarning("No platform ARN configured for platform {Platform}", device.Platform);
                            failureCount++;
                            errors.Add($"Platform {device.Platform} not configured");
                            continue;
                        }

                        // Create or get SNS endpoint
                        var endpointArn = await GetOrCreateSnsEndpointAsync(deviceToken, platformArn, device.Platform);

                        if (string.IsNullOrEmpty(endpointArn))
                        {
                            failureCount++;
                            errors.Add($"Failed to create SNS endpoint for device {deviceToken}");
                            continue;
                        }

                        // Send push notification via SNS
                        var message = JsonSerializer.Serialize(new
                        {
                            @default = request.Message,
                            APNS = JsonSerializer.Serialize(new { aps = new { alert = request.Title, sound = "default" } }),
                            GCM = JsonSerializer.Serialize(new { notification = new { title = request.Title, body = request.Message } })
                        });

                        var publishRequest = new PublishRequest
                        {
                            TargetArn = endpointArn,
                            Message = message,
                            MessageStructure = "json"
                        };

                        var response = await _snsClient.PublishAsync(publishRequest);
                        
                        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                        {
                            successCount++;
                            // Update device last used timestamp
                            await _deviceRegistrationService.UpdateDeviceLastUsedAsync(request.UserId, deviceToken);
                        }
                        else
                        {
                            failureCount++;
                            errors.Add($"Failed to send to device {deviceToken}: {response.HttpStatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        errors.Add($"Error sending to device {deviceToken}: {ex.Message}");
                        _logger.LogError(ex, "Error sending push notification to device {DeviceToken}", deviceToken);
                    }
                }

                return new DeliveryResult
                {
                    Success = successCount > 0,
                    ErrorMessage = failureCount > 0 ? string.Join("; ", errors) : null,
                    ExternalId = $"{successCount}/{deviceTokens.Count} devices"
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

        private async Task<string?> GetOrCreateSnsEndpointAsync(string deviceToken, string platformArn, string platform)
        {
            try
            {
                // Check if endpoint already exists (could cache this)
                // For now, create endpoint each time (in production, cache endpoint ARNs)
                var createEndpointRequest = new CreatePlatformEndpointRequest
                {
                    PlatformApplicationArn = platformArn,
                    Token = deviceToken
                };

                var response = await _snsClient.CreatePlatformEndpointAsync(createEndpointRequest);
                return response.EndpointArn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SNS endpoint for device token {DeviceToken}", deviceToken);
                return null;
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

