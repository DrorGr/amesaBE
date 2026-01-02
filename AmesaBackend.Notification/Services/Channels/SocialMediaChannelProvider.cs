using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Notification.Services.Interfaces;
using AmesaBackend.Shared.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AmesaBackend.Notification.Services.Channels
{
    public class SocialMediaChannelProvider : IChannelProvider
    {
        public string ChannelName => NotificationChannelConstants.SocialMedia;

        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialMediaChannelProvider> _logger;
        private readonly NotificationDbContext _context;
        private readonly IHttpRequest _httpRequest;
        private readonly HttpClient _httpClient;

        public SocialMediaChannelProvider(
            IConfiguration configuration,
            ILogger<SocialMediaChannelProvider> logger,
            NotificationDbContext context,
            IHttpRequest httpRequest,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _httpRequest = httpRequest;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                // Social media channels are typically for public announcements only
                // Cannot send unsolicited DMs to users
                // This would post to a page/channel instead
                
                var socialLinks = await _context.SocialMediaLinks
                    .Where(l => l.UserId == request.UserId && l.Verified)
                    .ToListAsync();

                if (socialLinks.Count == 0)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User has not linked social media accounts"
                    };
                }

                // For now, return not implemented as Meta Graph API requires OAuth setup
                _logger.LogInformation("SocialMedia channel provider - Meta Graph API integration requires OAuth setup");
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = "Social media posting requires OAuth token management"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending social media notification for user {UserId}", request.UserId);
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences)
        {
            // Social media channel requires verified link
            return Task.FromResult(true);
        }

        public bool IsChannelEnabled(Guid userId)
        {
            // Check user channel preferences and verified link
            var preference = _context.UserChannelPreferences
                .FirstOrDefault(p => p.UserId == userId && p.Channel == NotificationChannelConstants.SocialMedia);
            
            if (preference != null && !preference.Enabled)
            {
                return false;
            }

            // Check if user has verified social media link
            return _context.SocialMediaLinks.Any(l => l.UserId == userId && l.Verified);
        }
    }
}

