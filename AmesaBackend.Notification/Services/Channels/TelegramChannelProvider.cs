using Telegram.Bot;
using Telegram.Bot.Types;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.Services.Channels
{
    public class TelegramChannelProvider : IChannelProvider
    {
        public string ChannelName => NotificationChannelConstants.Telegram;

        private readonly ITelegramBotClient _botClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramChannelProvider> _logger;
        private readonly NotificationDbContext _context;

        public TelegramChannelProvider(
            IConfiguration configuration,
            ILogger<TelegramChannelProvider> logger,
            NotificationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;

            var botToken = _configuration["NotificationChannels:Telegram:BotToken"];
            if (!string.IsNullOrEmpty(botToken) && botToken != "FROM_SECRETS")
            {
                _botClient = new TelegramBotClient(botToken);
            }
        }

        public async Task<DeliveryResult> SendAsync(NotificationRequest request)
        {
            try
            {
                if (_botClient == null)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "Telegram bot token not configured"
                    };
                }

                // Get user's Telegram link
                var telegramLink = await _context.TelegramUserLinks
                    .FirstOrDefaultAsync(l => l.UserId == request.UserId && l.Verified);

                if (telegramLink == null)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        ErrorMessage = "User has not linked Telegram account"
                    };
                }

                // Format message
                var message = $"*{request.Title}*\n\n{request.Message}";
                
                // Send message via Telegram Bot API
                var sentMessage = await _botClient.SendTextMessageAsync(
                    chatId: telegramLink.ChatId,
                    text: message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

                return new DeliveryResult
                {
                    Success = true,
                    ExternalId = sentMessage.MessageId.ToString(),
                    Cost = 0m // Telegram Bot API is free
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Telegram notification for user {UserId}", request.UserId);
                return new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePreferencesAsync(Guid userId, NotificationPreferences preferences)
        {
            // Telegram channel requires verified link
            return Task.FromResult(true);
        }

        public bool IsChannelEnabled(Guid userId)
        {
            // Check user channel preferences and verified link
            var preference = _context.UserChannelPreferences
                .FirstOrDefault(p => p.UserId == userId && p.Channel == NotificationChannelConstants.Telegram);
            
            if (preference != null && !preference.Enabled)
            {
                return false;
            }

            // Check if user has verified Telegram link
            return _context.TelegramUserLinks.Any(l => l.UserId == userId && l.Verified);
        }
    }
}

