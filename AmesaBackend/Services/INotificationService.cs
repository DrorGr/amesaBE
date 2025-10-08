namespace AmesaBackend.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Guid userId, string title, string message, string type);
        Task SendLotteryWinnerNotificationAsync(Guid userId, string houseTitle, string ticketNumber);
        Task SendLotteryEndedNotificationAsync(Guid userId, string houseTitle, string? winnerName);
    }
}
