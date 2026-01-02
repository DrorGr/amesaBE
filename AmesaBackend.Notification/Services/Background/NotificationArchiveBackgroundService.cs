using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AmesaBackend.Notification.Services.Background
{
    /// <summary>
    /// Background service that runs daily to archive old notifications and clean up read history
    /// </summary>
    public class NotificationArchiveBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationArchiveBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromDays(1); // Run daily
        
        public NotificationArchiveBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationArchiveBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationArchiveBackgroundService started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var archiveService = scope.ServiceProvider.GetRequiredService<INotificationArchiveService>();
                    
                    _logger.LogInformation("Starting notification archive process");
                    
                    // Archive notifications older than 90 days
                    var archivedCount = await archiveService.ArchiveOldNotificationsAsync(90);
                    _logger.LogInformation("Archived {Count} notifications", archivedCount);
                    
                    // Clean up read history older than 180 days
                    var cleanedCount = await archiveService.CleanupReadHistoryAsync(180);
                    _logger.LogInformation("Cleaned up {Count} read history records", cleanedCount);
                    
                    _logger.LogInformation("Notification archive process completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in notification archive background service");
                }
                
                // Wait until next day
                await Task.Delay(_period, stoppingToken);
            }
            
            _logger.LogInformation("NotificationArchiveBackgroundService stopped");
        }
    }
}

