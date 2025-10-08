using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Services
{
    public class LotteryDrawService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LotteryDrawService> _logger;

        public LotteryDrawService(IServiceProvider serviceProvider, ILogger<LotteryDrawService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    // TODO: Implement lottery draw logic
                    _logger.LogInformation("Lottery draw service running...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in lottery draw service");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
