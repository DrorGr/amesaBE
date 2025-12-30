using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Lottery.Services;

public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _cleanupInterval;
    private readonly int _batchSize;
    private readonly int _retentionDays;

    public ReservationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ReservationCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var intervalHours = _configuration.GetValue<int>("ReservationCleanup:CleanupIntervalHours", 1);
        _cleanupInterval = TimeSpan.FromHours(intervalHours);
        _batchSize = _configuration.GetValue<int>("ReservationCleanup:BatchSize", 100);
        _retentionDays = _configuration.GetValue<int>("ReservationCleanup:RetentionDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("ReservationCleanup:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("ReservationCleanupService is disabled in configuration");
            return;
        }

        _logger.LogInformation("ReservationCleanupService started with cleanup interval: {Interval} hours, batch size: {BatchSize}, retention: {RetentionDays} days", 
            _cleanupInterval.TotalHours, _batchSize, _retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredReservationsAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ReservationCleanupService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReservationCleanupService");
                // Continue running even if there's an error
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        _logger.LogInformation("ReservationCleanupService stopped");
    }

    private async Task CleanupExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();

        try
        {
            // Update expired pending reservations to 'expired' status
            var expiredReservations = await dbContext.TicketReservations
                .Where(r => r.Status == "pending" && r.ExpiresAt < DateTime.UtcNow)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (!expiredReservations.Any())
            {
                _logger.LogDebug("No expired reservations to clean up");
                return;
            }

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = "expired";
                reservation.UpdatedAt = DateTime.UtcNow;
            }

            var expiredCount = await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated {Count} expired reservations to 'expired' status", expiredCount);

            // Optionally delete old expired reservations (older than retention days)
            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
            var oldExpiredReservations = await dbContext.TicketReservations
                .Where(r => r.Status == "expired" && r.UpdatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldExpiredReservations.Any())
            {
                dbContext.TicketReservations.RemoveRange(oldExpiredReservations);
                var deletedCount = await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted {Count} old expired reservations (older than {RetentionDays} days)", 
                    deletedCount, _retentionDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired reservations");
            throw;
        }
    }
}
