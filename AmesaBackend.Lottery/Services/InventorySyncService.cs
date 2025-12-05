using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using StackExchange.Redis;

namespace AmesaBackend.Lottery.Services
{
    public class InventorySyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InventorySyncService> _logger;

        public InventorySyncService(
            IServiceProvider serviceProvider,
            ILogger<InventorySyncService> logger)
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
                    var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                    var inventoryManager = scope.ServiceProvider.GetRequiredService<IRedisInventoryManager>();

                    var activeHouses = await context.Houses
                        .Where(h => h.Status == "Active" && h.LotteryEndDate > DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var house in activeHouses)
                    {
                        try
                        {
                            await SyncHouseInventoryAsync(house, context, redis, inventoryManager, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error syncing inventory for house {HouseId}", house.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in InventorySyncService");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task SyncHouseInventoryAsync(
            House house,
            LotteryDbContext context,
            IConnectionMultiplexer redis,
            IRedisInventoryManager inventoryManager,
            CancellationToken cancellationToken)
        {
            // Get actual counts from database
            var soldCount = await context.LotteryTickets
                .Where(t => t.HouseId == house.Id && t.Status == "Active")
                .CountAsync(cancellationToken);

            var reservedCount = await context.TicketReservations
                .Where(r => r.HouseId == house.Id && r.Status == "pending")
                .SumAsync(r => (int?)r.Quantity, cancellationToken) ?? 0;

            var actualAvailable = house.TotalTickets - soldCount - reservedCount;

            // Get Redis counts
            var redisAvailable = await inventoryManager.GetAvailableCountAsync(house.Id);

            var drift = Math.Abs(actualAvailable - redisAvailable);

            if (drift > 0)
            {
                _logger.LogWarning(
                    "Inventory drift detected for house {HouseId}: Database={DbAvailable}, Redis={RedisAvailable}, Drift={Drift}",
                    house.Id, actualAvailable, redisAvailable, drift);

                // Fix Redis by setting correct value
                var db = redis.GetDatabase();
                var houseKey = $"lottery:inventory:{house.Id}";
                await db.StringSetAsync(houseKey, actualAvailable);

                var reservedKey = $"lottery:inventory:{house.Id}:reserved";
                await db.StringSetAsync(reservedKey, reservedCount);

                var soldKey = $"lottery:inventory:{house.Id}:sold";
                await db.StringSetAsync(soldKey, soldCount);

                _logger.LogInformation(
                    "Fixed inventory drift for house {HouseId}: Set available={Available}, reserved={Reserved}, sold={Sold}",
                    house.Id, actualAvailable, reservedCount, soldCount);
            }
        }
    }
}








