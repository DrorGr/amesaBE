namespace AmesaBackend.Lottery.Services;

public class LotteryService : ILotteryService
{
    private readonly ILogger<LotteryService> _logger;

    public LotteryService(ILogger<LotteryService> logger)
    {
        _logger = logger;
    }

    // Implementation methods can be added here as needed
}

