using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LotterySettings _settings;
        private readonly ILogger<ReservationCleanupService> _logger;
        private readonly IConnectionMultiplexer? _redis;

        public ReservationCleanupService(
            IServiceProvider serviceProvider,
            IOptions<LotterySettings> settings,
            ILogger<ReservationCleanupService> logger,
            IConnectionMultiplexer? redis = null)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Acquire distributed lock to prevent concurrent cleanup
                    var lockKey = "reservation:cleanup:lock";
                    var lockAcquired = await AcquireDistributedLockAsync(lockKey, TimeSpan.FromMinutes(5));
                    
                    if (!lockAcquired)
                    {
                        _logger.LogDebug("Could not acquire cleanup lock, another instance may be running");
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        continue;
                    }

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
                        var inventoryManager = scope.ServiceProvider.GetRequiredService<IRedisInventoryManager>();

                        // Use execution strategy to wrap the transaction so retries are supported
                        var strategy = context.Database.CreateExecutionStrategy();
                        await strategy.ExecuteAsync(async () =>
                        {
                            // Use ReadCommitted instead of Serializable to reduce lock contention and deadlock potential
                            await using var transaction = await context.Database.BeginTransactionAsync(
                                System.Data.IsolationLevel.ReadCommitted, stoppingToken);
                            try
                            {
                                // Re-query within transaction to get latest state (prevent race conditions)
                                var expiredReservations = await context.TicketReservations
                                    .Where(r => r.Status == "pending" && r.ExpiresAt <= DateTime.UtcNow)
                                    .ToListAsync(stoppingToken);

                                foreach (var reservation in expiredReservations)
                                {
                                    try
                                    {
                                        // Idempotency check: Verify reservation is still pending
                                        var currentReservation = await context.TicketReservations
                                            .FirstOrDefaultAsync(r => r.Id == reservation.Id, stoppingToken);

                                        if (currentReservation == null || currentReservation.Status != "pending")
                                        {
                                            _logger.LogDebug(
                                                "Reservation {ReservationId} already processed (status: {Status}), skipping",
                                                reservation.Id, currentReservation?.Status ?? "deleted");
                                            continue;
                                        }

                                        // Release inventory
                                        var inventoryReleased = await inventoryManager.ReleaseInventoryAsync(
                                            reservation.HouseId,
                                            reservation.Quantity);

                                        if (!inventoryReleased)
                                        {
                                            _logger.LogWarning(
                                                "Failed to release inventory for reservation {ReservationId}, house {HouseId}",
                                                reservation.Id, reservation.HouseId);
                                            // Continue anyway - mark as expired
                                        }

                                        // Update reservation status atomically within transaction
                                        currentReservation.Status = "expired";
                                        currentReservation.UpdatedAt = DateTime.UtcNow;

                                        _logger.LogInformation(
                                            "Expired reservation {ReservationId} for house {HouseId}, released {Quantity} tickets",
                                            reservation.Id, reservation.HouseId, reservation.Quantity);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex,
                                            "Error cleaning up expired reservation {ReservationId}",
                                            reservation.Id);
                                        // Continue with next reservation
                                    }
                                }

                                await context.SaveChangesAsync(stoppingToken);
                                await transaction.CommitAsync(stoppingToken);

                                if (expiredReservations.Any())
                                {
                                    _logger.LogInformation(
                                        "Cleaned up {Count} expired reservations",
                                        expiredReservations.Count);
                                }
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    await transaction.RollbackAsync(stoppingToken);
                                }
                                catch (Exception rollbackEx)
                                {
                                    _logger.LogError(rollbackEx, "Error rolling back transaction in ReservationCleanupService");
                                }

                                _logger.LogError(ex, "Error in reservation cleanup transaction, rolled back");
                                throw;
                            }
                        });
                    }
                    finally
                    {
                        // Release distributed lock - protected with try-catch to ensure it always executes
                        try
                        {
                            await ReleaseDistributedLockAsync(lockKey);
                        }
                        catch (Exception lockEx)
                        {
                            _logger.LogWarning(lockEx, "Failed to release lock {LockKey} in finally block", lockKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReservationCleanupService");
                }

                await Task.Delay(TimeSpan.FromMinutes(_settings.BackgroundServices.ReservationCleanupIntervalMinutes), stoppingToken);
            }
        }

        private async Task<bool> AcquireDistributedLockAsync(string lockKey, TimeSpan expiry)
        {
            if (_redis == null)
            {
                // If Redis not available, proceed without lock (single instance scenario)
                return true;
            }

            try
            {
                var db = _redis.GetDatabase();
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
                return true; // Fail-open: proceed without lock if Redis unavailable
            }
        }

        private async Task ReleaseDistributedLockAsync(string lockKey)
        {
            if (_redis == null)
            {
                return;
            }

            try
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(lockKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release distributed lock {LockKey}", lockKey);
            }
        }
    }
}












