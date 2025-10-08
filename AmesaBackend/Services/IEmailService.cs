namespace AmesaBackend.Services
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string email, string token);
        Task SendPasswordResetAsync(string email, string token);
        Task SendWelcomeEmailAsync(string email, string name);
        Task SendLotteryWinnerNotificationAsync(string email, string name, string houseTitle, string ticketNumber);
        Task SendLotteryEndedNotificationAsync(string email, string name, string houseTitle, string? winnerName);
        Task SendGeneralNotificationAsync(string email, string subject, string body);
    }
}
