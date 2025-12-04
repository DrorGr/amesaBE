using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;

namespace AmesaBackend.Lottery.Services
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReservationCleanupService> _logger;

        public ReservationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ReservationCleanupService> logger)
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
                    var inventoryManager = scope.ServiceProvider.GetRequiredService<IRedisInventoryManager>();

                    var expiredReservations = await context.TicketReservations
                        .Where(r => r.Status == "pending" && r.ExpiresAt <= DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var reservation in expiredReservations)
                    {
                        try
                        {
                            // Release inventory
                            await inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);

                            // Update reservation status
                            reservation.Status = "expired";
                            reservation.UpdatedAt = DateTime.UtcNow;
                            
                            _logger.LogInformation(
                                "Expired reservation {ReservationId} for house {HouseId}, released {Quantity} tickets",
                                reservation.Id, reservation.HouseId, reservation.Quantity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                                "Error cleaning up expired reservation {ReservationId}", 
                                reservation.Id);
                        }
                    }

                    if (expiredReservations.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReservationCleanupService");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}





