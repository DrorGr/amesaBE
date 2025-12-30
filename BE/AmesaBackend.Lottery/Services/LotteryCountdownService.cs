using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Hubs;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Lottery.Services;

public class LotteryCountdownService : BackgroundService
{
    private readonly IHubContext<LotteryHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LotteryCountdownService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _updateInterval;

    public LotteryCountdownService(
        IHubContext<LotteryHub> hubContext,
        IServiceProvider serviceProvider,
        ILogger<LotteryCountdownService> logger,
        IConfiguration configuration)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var intervalSeconds = _configuration.GetValue<int>("LotteryCountdown:UpdateIntervalSeconds", 1);
        _updateInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("LotteryCountdown:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("LotteryCountdownService is disabled in configuration");
            return;
        }

        _logger.LogInformation("LotteryCountdownService started with update interval: {Interval} seconds", _updateInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCountdownsAsync(stoppingToken);
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LotteryCountdownService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LotteryCountdownService");
                // Continue running even if there's an error
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        _logger.LogInformation("LotteryCountdownService stopped");
    }

    private async Task ProcessCountdownsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();

        try
        {
            // Query active houses with upcoming draws
            var activeHouses = await dbContext.Houses
                .Where(h => h.Status == LotteryStatus.Active && h.LotteryEndDate > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (!activeHouses.Any())
            {
                // No active lotteries, skip broadcasting
                return;
            }

            var broadcastTasks = activeHouses.Select(house => BroadcastCountdownForHouse(house, cancellationToken));
            await Task.WhenAll(broadcastTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing countdowns");
            throw;
        }
    }

    private async Task BroadcastCountdownForHouse(House house, CancellationToken cancellationToken)
    {
        try
        {
            var timeRemaining = house.LotteryEndDate - DateTime.UtcNow;
            
            if (timeRemaining <= TimeSpan.Zero)
            {
                // Lottery has ended, don't broadcast
                return;
            }

            var countdownUpdate = new CountdownUpdate
            {
                HouseId = house.Id,
                TimeRemaining = timeRemaining,
                Days = timeRemaining.Days,
                Hours = timeRemaining.Hours,
                Minutes = timeRemaining.Minutes,
                Seconds = timeRemaining.Seconds,
                IsActive = true,
                IsEnded = false,
                LotteryEndDate = house.LotteryEndDate
            };

            await _hubContext.BroadcastCountdownUpdate(house.Id, countdownUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting countdown for house {HouseId}", house.Id);
        }
    }
}
