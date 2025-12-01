using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Shared.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.Services.Channels
{
    public class SMSChannelProvider : IChannelProvider
    {
        public string ChannelName => NotificationChannelConstants.SMS;

        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SMSChannelProvider> _logger;
        private readonly NotificationDbContext _context;
        private readonly IHttpRequest _httpRequest;

        private readonly string _senderId;
        private readonly int _maxLength;

        public SMSChannelProvider(
            IAmazonSimpleNotificationService snsClient,
            IConfiguration configuration,
            ILogger<SMSChannelProvider> logger,
            NotificationDbContext context,
            IHttpRequest httpRequest)
        {
            _snsClient = snsClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _httpRequest = httpRequest;

            var smsConfig = _configuration.GetSection("NotificationChannels:SMS");
            _senderId = smsConfig["SenderId"] ?? "Amesa";
            _maxLength = smsConfig.GetValue<int>("MaxLength", 160);
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                // Fetch user phone number from Auth service
                var userData = await _httpRequest.GetRequest<Dictionary<string, object>>(
                    $"{_configuration["Services:AuthService:Url"]}/api/v1/users/{request.UserId}",
                    _configuration["JwtSettings:SecretKey"] ?? "");

                if (userData == null || !userData.ContainsKey("phoneNumber"))
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User phone number not found"
                    };
                }

                var phoneNumber = userData["phoneNumber"]?.ToString();
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User phone number is empty"
                    };
                }

                // Format phone number (ensure it starts with +)
                if (!phoneNumber.StartsWith("+"))
                {
                    phoneNumber = "+" + phoneNumber;
                }

                // Truncate message if too long
                var message = request.Message;
                if (message.Length > _maxLength)
                {
                    message = message.Substring(0, _maxLength - 3) + "...";
                }

                // Create SNS publish request
                var publishRequest = new PublishRequest
                {
                    PhoneNumber = phoneNumber,
                    Message = message,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        ["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue
                        {
                            StringValue = _senderId,
                            DataType = "String"
                        },
                        ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue
                        {
                            StringValue = "Transactional",
                            DataType = "String"
                        }
                    }
                };

                // Send SMS via AWS SNS
                var response = await _snsClient.PublishAsync(publishRequest);

                // Calculate cost (approximate: $0.00645 per SMS in most regions)
                var cost = 0.00645m;

                return new DeliveryResult
                {
                    Success = true,
                    ExternalId = response.MessageId,
                    Cost = cost
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification for user {UserId}", request.UserId);
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences)
        {
            // SMS channel requires phone number
            return Task.FromResult(true);
        }

        public bool IsChannelEnabled(Guid userId)
        {
            // Check user channel preferences
            var preference = _context.UserChannelPreferences
                .FirstOrDefault(p => p.UserId == userId && p.Channel == NotificationChannelConstants.SMS);
            
            return preference == null || preference.Enabled;
        }
    }
}

