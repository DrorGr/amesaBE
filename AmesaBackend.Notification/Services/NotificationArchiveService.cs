using AmesaBackend.Notification.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.Services
{
    public interface INotificationArchiveService
    {
        Task<int> ArchiveOldNotificationsAsync(int daysOld = 90);
        Task<int> CleanupReadHistoryAsync(int daysOld = 180);
    }

    public class NotificationArchiveService : INotificationArchiveService
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationArchiveService> _logger;
        
        public NotificationArchiveService(
            NotificationDbContext context,
            ILogger<NotificationArchiveService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        /// <summary>
        /// Archives old notifications by marking them as deleted (soft delete)
        /// </summary>
        /// <param name="daysOld">Notifications older than this many days will be archived</param>
        /// <returns>Number of notifications archived</returns>
        public async Task<int> ArchiveOldNotificationsAsync(int daysOld = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                
                // Mark old notifications as archived (soft delete)
                var count = await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE user_notifications 
                    SET is_deleted = true, 
                        deleted_at = NOW(),
                        deleted_by = 'system'
                    WHERE created_at < {0} 
                    AND is_deleted = false", cutoffDate);
                
                _logger.LogInformation("Archived {Count} notifications older than {DaysOld} days", count, daysOld);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving old notifications");
                throw;
            }
        }
        
        /// <summary>
        /// Cleans up old read history records
        /// </summary>
        /// <param name="daysOld">Read history older than this many days will be deleted</param>
        /// <returns>Number of read history records deleted</returns>
        public async Task<int> CleanupReadHistoryAsync(int daysOld = 180)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                
                var count = await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM notification_read_history 
                    WHERE read_at < {0}", cutoffDate);
                
                _logger.LogInformation("Cleaned up {Count} read history records older than {DaysOld} days", count, daysOld);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up read history");
                throw;
            }
        }
    }
}

