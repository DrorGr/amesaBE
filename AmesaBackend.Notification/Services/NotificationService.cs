using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Notification.Services.Interfaces;
using AmesaBackend.Shared.Rest;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AmesaBackend.Notification.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHttpRequest _httpRequest;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public NotificationService(
            NotificationDbContext context,
            IEmailService emailService,
            IHttpRequest httpRequest,
            IConfiguration configuration,
            ILogger<NotificationService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _emailService = emailService;
            _httpRequest = httpRequest;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task SendNotificationAsync(Guid userId, string title, string message, string type)
        {
            var notification = new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }

        public async Task SendLotteryWinnerNotificationAsync(Guid userId, string houseTitle, string ticketNumber)
        {
            try
            {
                // Get user info from Auth Service via HTTP
                var authServiceUrl = _configuration["Services:AuthService:Url"] ?? "http://auth-service:8080";
                var userResponse = await _httpRequest.GetRequest<ApiResponse<UserInfoDto>>(
                    $"{authServiceUrl}/api/v1/auth/users/{userId}", 
                    "");

                string userEmail = "";
                string userName = "User";

                if (userResponse?.Success == true && userResponse.Data != null)
                {
                    userEmail = userResponse.Data.Email ?? "";
                    userName = userResponse.Data.FirstName ?? userResponse.Data.Username ?? "User";
                }

                // Send email via EmailService if email is available
                if (!string.IsNullOrEmpty(userEmail))
                {
                    try
                    {
                        await _emailService.SendLotteryWinnerNotificationAsync(
                            userEmail,
                            userName,
                            houseTitle,
                            ticketNumber);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send winner email to {Email} for user {UserId}", userEmail, userId);
                    }
                }

                // Use NotificationOrchestrator for multi-channel delivery
                var orchestrator = _serviceProvider.GetRequiredService<INotificationOrchestrator>();
                var request = new NotificationRequest
                {
                    UserId = userId,
                    Type = NotificationTypeConstants.LotteryWinnerSelected,
                    Title = "🎉 Congratulations! You Won!",
                    Message = $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
                    Channel = "email",
                    Data = new Dictionary<string, object>
                    {
                        { "houseTitle", houseTitle },
                        { "ticketNumber", ticketNumber }
                    }
                };

                await orchestrator.SendMultiChannelAsync(userId, request, new List<string> { "email", "webpush", "push" });

                // Also create notification record
                await SendNotificationAsync(
                    userId,
                    "🎉 Congratulations! You Won!",
                    $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
                    NotificationTypeConstants.LotteryWinnerSelected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending lottery winner notification to user {UserId}", userId);
                // Still create notification record even if email/orchestrator fails
                await SendNotificationAsync(
                    userId,
                    "🎉 Congratulations! You Won!",
                    $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
                    NotificationTypeConstants.LotteryWinnerSelected);
            }
        }

        public async Task SendLotteryEndedNotificationAsync(Guid userId, string houseTitle, string? winnerName)
        {
            try
            {
                // Get user info from Auth Service via HTTP
                var authServiceUrl = _configuration["Services:AuthService:Url"] ?? "http://auth-service:8080";
                var userResponse = await _httpRequest.GetRequest<ApiResponse<UserInfoDto>>(
                    $"{authServiceUrl}/api/v1/auth/users/{userId}", 
                    "");

                string userEmail = "";
                string userName = "User";

                if (userResponse?.Success == true && userResponse.Data != null)
                {
                    userEmail = userResponse.Data.Email ?? "";
                    userName = userResponse.Data.FirstName ?? userResponse.Data.Username ?? "User";
                }

                var message = winnerName != null
                    ? $"The lottery for {houseTitle} has ended. Winner: {winnerName}"
                    : $"The lottery for {houseTitle} has ended and was cancelled due to insufficient participation.";

                // Send email via EmailService if email is available
                if (!string.IsNullOrEmpty(userEmail))
                {
                    try
                    {
                        await _emailService.SendLotteryEndedNotificationAsync(
                            userEmail,
                            userName,
                            houseTitle,
                            winnerName);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send lottery ended email to {Email} for user {UserId}", userEmail, userId);
                    }
                }

                // Use NotificationOrchestrator for multi-channel delivery
                var orchestrator = _serviceProvider.GetRequiredService<INotificationOrchestrator>();
                var request = new NotificationRequest
                {
                    UserId = userId,
                    Type = NotificationTypeConstants.LotteryDrawCompleted,
                    Title = "Lottery Ended",
                    Message = message,
                    Channel = "email",
                    Data = new Dictionary<string, object>
                    {
                        { "houseTitle", houseTitle },
                        { "winnerName", winnerName ?? "" }
                    }
                };

                await orchestrator.SendMultiChannelAsync(userId, request, new List<string> { "email", "webpush" });

                // Also create notification record
                await SendNotificationAsync(
                    userId,
                    "Lottery Ended",
                    message,
                    NotificationTypeConstants.LotteryDrawCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending lottery ended notification to user {UserId}", userId);
                // Still create notification record even if email/orchestrator fails
                var message = winnerName != null
                    ? $"The lottery for {houseTitle} has ended. Winner: {winnerName}"
                    : $"The lottery for {houseTitle} has ended and was cancelled due to insufficient participation.";
                await SendNotificationAsync(
                    userId,
                    "Lottery Ended",
                    message,
                    NotificationTypeConstants.LotteryDrawCompleted);
            }
        }
    }

    // DTOs for Auth Service response
    public class UserInfoDto
    {
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
    }
}

