using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace AmesaBackend.Lottery.Services;

public class InventorySyncService : BackgroundService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventorySyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _syncInterval;
    private readonly TimeSpan _cacheTTL;

    public InventorySyncService(
        IServiceProvider serviceProvider,
        ILogger<InventorySyncService> logger,
        IConfiguration configuration,
        IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var syncIntervalMinutes = _configuration.GetValue<int>("InventorySync:SyncIntervalMinutes", 5);
        _syncInterval = TimeSpan.FromMinutes(syncIntervalMinutes);
        var cacheTTLMinutes = _configuration.GetValue<int>("InventorySync:CacheTTLMinutes", 5);
        _cacheTTL = TimeSpan.FromMinutes(cacheTTLMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("InventorySync:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("InventorySyncService is disabled in configuration");
            return;
        }

        _logger.LogInformation("InventorySyncService started with sync interval: {Interval} minutes, cache TTL: {TTL} minutes", 
            _syncInterval.TotalMinutes, _cacheTTL.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncInventoryAsync(stoppingToken);
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("InventorySyncService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InventorySyncService");
                // Continue running even if there's an error
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        _logger.LogInformation("InventorySyncService stopped");
    }

    private async Task SyncInventoryAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
        
        // Get Redis from service provider (may be null if not configured)
        var redis = _redis ?? scope.ServiceProvider.GetService<IConnectionMultiplexer>();
        
        if (redis == null)
        {
            _logger.LogWarning("Redis is not configured, skipping inventory sync");
            return;
        }

        var database = redis.GetDatabase();

        try
        {
            // Check Redis connection
            if (!redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected, skipping inventory sync");
                return;
            }

            // Get all active houses
            var activeHouses = await dbContext.Houses
                .Where(h => h.Status == LotteryStatus.Active || h.Status == LotteryStatus.Upcoming)
                .ToListAsync(cancellationToken);

            if (!activeHouses.Any())
            {
                _logger.LogDebug("No active houses to sync inventory for");
                return;
            }

            _logger.LogInformation("Syncing inventory for {Count} houses", activeHouses.Count);

            foreach (var house in activeHouses)
            {
                try
                {
                    await SyncHouseInventoryAsync(house, dbContext, database, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing inventory for house {HouseId}", house.Id);
                    // Continue with next house
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory");
            throw;
        }
    }

    private async Task SyncHouseInventoryAsync(
        House house,
        LotteryDbContext dbContext,
        IDatabase database,
        CancellationToken cancellationToken)
    {
        try
        {
            // Query ticket counts
            var ticketsSold = await dbContext.LotteryTickets
                .Where(t => t.HouseId == house.Id && t.Status == TicketStatus.Active)
                .CountAsync(cancellationToken);

            var availableTickets = house.TotalTickets - ticketsSold;
            var lastUpdated = DateTime.UtcNow;

            // Create inventory data
            var inventoryData = new
            {
                availableTickets = availableTickets,
                totalTickets = house.TotalTickets,
                ticketsSold = ticketsSold,
                lastUpdated = lastUpdated
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(inventoryData);

            // Store in Redis with TTL
            var key = $"house_inventory:{house.Id}";
            await database.StringSetAsync(key, json, _cacheTTL);

            // Also store tickets sold count separately (optional, for quick access)
            var ticketsSoldKey = $"house_tickets_sold:{house.Id}";
            await database.StringSetAsync(ticketsSoldKey, ticketsSold, _cacheTTL);

            _logger.LogDebug("Synced inventory for house {HouseId}: {Available}/{Total} tickets available", 
                house.Id, availableTickets, house.TotalTickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing inventory for house {HouseId}", house.Id);
            throw;
        }
    }
}
