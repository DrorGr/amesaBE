using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Configuration;
using StackExchange.Redis;

namespace AmesaBackend.Lottery.Services
{
    public class InventorySyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LotterySettings _settings;
        private readonly ILogger<InventorySyncService> _logger;

        public InventorySyncService(
            IServiceProvider serviceProvider,
            IOptions<LotterySettings> settings,
            ILogger<InventorySyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
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

                await Task.Delay(TimeSpan.FromMinutes(_settings.BackgroundServices.InventorySyncIntervalMinutes), stoppingToken);
            }
        }

        private async Task SyncHouseInventoryAsync(
            House house,
            LotteryDbContext context,
            IConnectionMultiplexer redis,
            IRedisInventoryManager inventoryManager,
            CancellationToken cancellationToken)
        {
            // Acquire distributed lock for this house to prevent concurrent sync operations
            var lockKey = $"inventory:sync:lock:{house.Id}";
            var lockAcquired = await AcquireDistributedLockAsync(redis, lockKey, TimeSpan.FromMinutes(2));
            
            if (!lockAcquired)
            {
                _logger.LogDebug("Could not acquire sync lock for house {HouseId}, skipping this sync cycle", house.Id);
                return;
            }

            try
            {
                // Wrap sync in transaction for consistency
                using var transaction = await context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);

                try
                {
                    // Get actual counts from database
                    // Exclude processing reservations (they're in-flight and shouldn't be counted)
                    var soldCount = await context.LotteryTickets
                        .Where(t => t.HouseId == house.Id && t.Status == "Active")
                        .CountAsync(cancellationToken);

                    // Only count pending reservations (exclude processing ones)
                    var reservedCount = await context.TicketReservations
                        .Where(r => r.HouseId == house.Id && r.Status == "pending")
                        .SumAsync(r => (int?)r.Quantity, cancellationToken) ?? 0;

                    // Validate counts before overwriting Redis
                    if (soldCount < 0 || reservedCount < 0)
                    {
                        _logger.LogError(
                            "Invalid counts detected for house {HouseId}: sold={SoldCount}, reserved={ReservedCount}",
                            house.Id, soldCount, reservedCount);
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    var actualAvailable = house.TotalTickets - soldCount - reservedCount;

                    // Validate available count is non-negative
                    if (actualAvailable < 0)
                    {
                        _logger.LogError(
                            "Negative available count calculated for house {HouseId}: Total={Total}, Sold={Sold}, Reserved={Reserved}, Available={Available}",
                            house.Id, house.TotalTickets, soldCount, reservedCount, actualAvailable);
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    // Get Redis counts
                    var redisAvailable = await inventoryManager.GetAvailableCountAsync(house.Id);

                    var drift = Math.Abs(actualAvailable - redisAvailable);

                    // Only sync if drift exceeds threshold (to avoid unnecessary writes)
                    if (drift > 0)
                    {
                        _logger.LogWarning(
                            "Inventory drift detected for house {HouseId}: Database={DbAvailable}, Redis={RedisAvailable}, Drift={Drift}",
                            house.Id, actualAvailable, redisAvailable, drift);

                        // Validate before overwriting Redis
                        // Double-check that our calculated values are reasonable
                        if (actualAvailable > house.TotalTickets)
                        {
                            _logger.LogError(
                                "Calculated available count exceeds total tickets for house {HouseId}: Available={Available}, Total={Total}",
                                house.Id, actualAvailable, house.TotalTickets);
                            await transaction.RollbackAsync(cancellationToken);
                            return;
                        }

                        // Fix Redis by setting correct value atomically
                        var db = redis.GetDatabase();
                        var houseKey = $"lottery:inventory:{house.Id}";
                        var reservedKey = $"lottery:inventory:{house.Id}:reserved";
                        var soldKey = $"lottery:inventory:{house.Id}:sold";

                        // Use Lua script for atomic update
                        var script = @"
                            local houseKey = KEYS[1]
                            local reservedKey = KEYS[2]
                            local soldKey = KEYS[3]
                            local available = tonumber(ARGV[1])
                            local reserved = tonumber(ARGV[2])
                            local sold = tonumber(ARGV[3])
                            
                            -- Validate values are non-negative
                            if available < 0 or reserved < 0 or sold < 0 then
                                return 0
                            end
                            
                            redis.call('SET', houseKey, available)
                            redis.call('SET', reservedKey, reserved)
                            redis.call('SET', soldKey, sold)
                            
                            return 1
                        ";

                        var result = await db.ScriptEvaluateAsync(
                            script,
                            keys: new RedisKey[] { houseKey, reservedKey, soldKey },
                            values: new RedisValue[] { actualAvailable, reservedCount, soldCount });

                        if ((int)result == 1)
                        {
                            await transaction.CommitAsync(cancellationToken);
                            _logger.LogInformation(
                                "Fixed inventory drift for house {HouseId}: Set available={Available}, reserved={Reserved}, sold={Sold}",
                                house.Id, actualAvailable, reservedCount, soldCount);
                        }
                        else
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogError(
                                "Failed to update Redis inventory for house {HouseId}: Validation failed",
                                house.Id);
                        }
                    }
                    else
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error in inventory sync transaction for house {HouseId}, rolled back", house.Id);
                    throw;
                }
            }
            finally
            {
                // Release distributed lock
                await ReleaseDistributedLockAsync(redis, lockKey);
            }
        }

        private async Task<bool> AcquireDistributedLockAsync(IConnectionMultiplexer redis, string lockKey, TimeSpan expiry)
        {
            try
            {
                var db = redis.GetDatabase();
                var lockValue = Guid.NewGuid().ToString();
                var acquired = await db.StringSetAsync(
                    lockKey, 
                    lockValue, 
                    expiry, 
                    When.NotExists);
                
                return acquired;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire distributed lock {LockKey}, proceeding without lock", lockKey);
                return false; // Fail-closed: don't sync if lock unavailable
            }
        }

        private async Task ReleaseDistributedLockAsync(IConnectionMultiplexer redis, string lockKey)
        {
            try
            {
                var db = redis.GetDatabase();
                await db.KeyDeleteAsync(lockKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release distributed lock {LockKey}", lockKey);
            }
        }
    }
}












