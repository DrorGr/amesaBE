namespace AmesaBackend.Lottery.Services;

public interface IGamificationService
{
    Task<object> GetUserGamificationDataAsync(Guid userId);
}

