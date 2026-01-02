using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Interfaces;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Lottery.Services.Background
{
    public class LotteryDrawService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LotteryDrawService> _logger;
        private readonly IEventPublisher? _eventPublisher;

        public LotteryDrawService(
            IServiceProvider serviceProvider, 
            ILogger<LotteryDrawService> logger,
            IEventPublisher? eventPublisher = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
                    // Get event publisher from scope (or use injected one)
                    var eventPublisher = _eventPublisher ?? scope.ServiceProvider.GetService<IEventPublisher>();

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
                            // Note: ConductDrawAsync already publishes LotteryDrawCompletedEvent and LotteryDrawWinnerSelectedEvent
                            var conductDrawRequest = new ConductDrawRequest
                            {
                                DrawMethod = "random",
                                DrawSeed = Guid.NewGuid().ToString()
                            };
                            
                            await lotteryService.ConductDrawAsync(draw.Id, conductDrawRequest);
                            
                            _logger.LogInformation("Draw {DrawId} completed successfully", draw.Id);
                            
                            // Additional event publishing can be done here if needed
                            // The main events are already published by ConductDrawAsync
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing draw {DrawId}", draw.Id);
                            // Mark draw as failed
                            draw.DrawStatus = "Failed";
                            await context.SaveChangesAsync(stoppingToken);
                            
                            // Optionally publish draw failed event
                            if (eventPublisher != null)
                            {
                                try
                                {
                                    await eventPublisher.PublishAsync(new LotteryDrawFailedEvent
                                    {
                                        DrawId = draw.Id,
                                        HouseId = draw.HouseId,
                                        FailureReason = ex.Message
                                    });
                                }
                                catch (Exception eventEx)
                                {
                                    _logger.LogError(eventEx, "Failed to publish draw failed event for draw {DrawId}", draw.Id);
                                }
                            }
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

