using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Hubs;
using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services
{
    public class LotteryCountdownService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LotteryCountdownService> _logger;

        public LotteryCountdownService(
            IServiceProvider serviceProvider,
            ILogger<LotteryCountdownService> logger)
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
                    var context = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LotteryHub>>();

                    var activeHouses = await context.Houses
                        .Where(h => h.Status == "Active" && h.LotteryEndDate > DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var house in activeHouses)
                    {
                        try
                        {
                            var timeRemaining = house.LotteryEndDate - DateTime.UtcNow;
                            
                            if (timeRemaining <= TimeSpan.Zero)
                            {
                                continue; // Skip ended lotteries
                            }

                            var countdownUpdate = new CountdownUpdate
                            {
                                HouseId = house.Id,
                                TimeRemaining = timeRemaining,
                                IsEnded = false,
                                LotteryEndDate = house.LotteryEndDate
                            };

                            await hubContext.Clients.Group($"lottery_{house.Id}")
                                .SendAsync("CountdownUpdated", countdownUpdate, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error broadcasting countdown for house {HouseId}", house.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LotteryCountdownService");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}








