using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services
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
                    var context = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
                    var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                    // Check for lotteries that need to be drawn
                    var pendingDraws = await context.LotteryDraws
                        .Where(d => d.DrawStatus == "Pending" && d.DrawDate <= DateTime.UtcNow)
                        .Include(d => d.House)
                        .ToListAsync(stoppingToken);

                    foreach (var draw in pendingDraws)
                    {
                        try
                        {
                            _logger.LogInformation("Processing draw {DrawId} for house {HouseId}", draw.Id, draw.HouseId);
                            
                            var lotteryService = scope.ServiceProvider.GetRequiredService<ILotteryService>();
                            
                            // Conduct the draw using the LotteryService
                            var conductDrawRequest = new ConductDrawRequest
                            {
                                DrawMethod = "random",
                                DrawSeed = Guid.NewGuid().ToString()
                            };
                            
                            await lotteryService.ConductDrawAsync(draw.Id, conductDrawRequest);
                            
                            _logger.LogInformation("Draw {DrawId} completed successfully", draw.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing draw {DrawId}", draw.Id);
                            // Mark draw as failed
                            draw.DrawStatus = "Failed";
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
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

