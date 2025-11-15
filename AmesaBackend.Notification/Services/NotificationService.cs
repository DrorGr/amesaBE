using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Shared.Rest;

namespace AmesaBackend.Notification.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHttpRequest _httpRequest;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            NotificationDbContext context,
            IEmailService emailService,
            IHttpRequest httpRequest,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _httpRequest = httpRequest;
            _configuration = configuration;
            _logger = logger;
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
            // Get user info from Auth Service via HTTP
            var authServiceUrl = _configuration["Services:AuthService:Url"] ?? "http://auth-service:8080";
            var userResponse = await _httpRequest.GetAsync<object>($"{authServiceUrl}/api/v1/auth/users/{userId}");

            if (userResponse.Success && userResponse.Value != null)
            {
                // Parse user data and send email
                // In a real implementation, you'd have a UserDto type
                // For now, we'll extract email from the response
                // This is a simplified version - in production, you'd have proper DTOs
            }

            await SendNotificationAsync(
                userId,
                "🎉 Congratulations! You Won!",
                $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
                "winner");
        }

        public async Task SendLotteryEndedNotificationAsync(Guid userId, string houseTitle, string? winnerName)
        {
            // Similar to above - get user info and send email
            var message = winnerName != null
                ? $"The lottery for {houseTitle} has ended. Winner: {winnerName}"
                : $"The lottery for {houseTitle} has ended and was cancelled due to insufficient participation.";

            await SendNotificationAsync(
                userId,
                "Lottery Ended",
                message,
                "lottery_ended");
        }
    }
}

