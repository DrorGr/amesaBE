using AmesaBackend.Data;
using AmesaBackend.Models;

namespace AmesaBackend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AmesaDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AmesaDbContext context,
            IEmailService emailService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
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
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            await _emailService.SendLotteryWinnerNotificationAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                houseTitle,
                ticketNumber);

            await SendNotificationAsync(
                userId,
                "🎉 Congratulations! You Won!",
                $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
                "winner");
        }

        public async Task SendLotteryEndedNotificationAsync(Guid userId, string houseTitle, string? winnerName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            await _emailService.SendLotteryEndedNotificationAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                houseTitle,
                winnerName);

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
