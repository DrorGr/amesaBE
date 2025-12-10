using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace AmesaBackend.Notification.Services
{
    public interface INotificationReadStateService
    {
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, string? deviceId = null, string? userAgent = null, string? channel = null);
        Task<int> MarkAllAsReadAsync(Guid userId, string? notificationTypeCode = null);
        Task<bool> MarkAsUnreadAsync(Guid notificationId, Guid userId);
        Task<NotificationReadHistory?> GetLastReadHistoryAsync(Guid notificationId);
        Task<List<NotificationReadHistory>> GetReadHistoryAsync(Guid notificationId);
        Task<bool> IsReadAsync(Guid notificationId, Guid userId);
        Task SyncReadStateAsync(Guid userId, List<Guid> readNotificationIds);
    }
    
    public class NotificationReadStateService : INotificationReadStateService
    {
        private readonly NotificationDbContext _context;
        private readonly IHubContext<NotificationHub>? _hubContext;
        private readonly ILogger<NotificationReadStateService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public NotificationReadStateService(
            NotificationDbContext context,
            IHubContext<NotificationHub>? hubContext,
            ILogger<NotificationReadStateService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public async Task<bool> MarkAsReadAsync(
            Guid notificationId, 
            Guid userId, 
            string? deviceId = null, 
            string? userAgent = null, 
            string? channel = null)
        {
            return await MarkAsReadAsyncInternal(notificationId, userId, deviceId, userAgent, channel, 0);
        }

        private async Task<bool> MarkAsReadAsyncInternal(
            Guid notificationId, 
            Guid userId, 
            string? deviceId = null, 
            string? userAgent = null, 
            string? channel = null,
            int retryCount = 0)
        {
            const int maxRetries = 3;
            
            try
            {
                // Get user's IP address
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                
                // Use optimistic concurrency to prevent race conditions
                // Filter out soft-deleted notifications
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted);
                
                if (notification == null)
                {
                    _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                    return false;
                }
                
                // Check if already read (optimize for common case)
                if (notification.IsRead)
                {
                    // Still record read history for analytics
                    await RecordReadHistoryAsync(notificationId, userId, deviceId, userAgent, channel, ipAddress, "auto");
                    return true;
                }
                
                // Mark as read with optimistic concurrency
                // Let EF Core manage RowVersion automatically via [Timestamp] attribute
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // Record read history
                await RecordReadHistoryAsync(notificationId, userId, deviceId, userAgent, channel, ipAddress, "manual");
                
                // Broadcast read state change to all user's devices via SignalR
                if (_hubContext != null)
                {
                    await _hubContext.Clients
                        .Group($"notifications_{userId}")
                        .SendAsync("NotificationReadStateChanged", new
                        {
                            NotificationId = notificationId,
                            IsRead = true,
                            ReadAt = notification.ReadAt,
                            DeviceId = deviceId
                        });
                }
                
                _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", notificationId, userId);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (retryCount < maxRetries)
                {
                    _logger.LogWarning(ex, "Concurrency conflict marking notification {NotificationId} as read. Retry {RetryCount}/{MaxRetries}", 
                        notificationId, retryCount + 1, maxRetries);
                    // Wait briefly before retry to allow other transaction to complete
                    await Task.Delay(100 * (retryCount + 1));
                    return await MarkAsReadAsyncInternal(notificationId, userId, deviceId, userAgent, channel, retryCount + 1);
                }
                else
                {
                    _logger.LogError(ex, "Max retries ({MaxRetries}) exceeded for marking notification {NotificationId} as read", 
                        maxRetries, notificationId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                throw;
            }
        }
        
        public async Task<int> MarkAllAsReadAsync(Guid userId, string? notificationTypeCode = null)
        {
            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
                var deviceId = _httpContextAccessor.HttpContext?.Request.Headers["X-Device-Id"].ToString();
                
                var query = _context.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
                
                if (!string.IsNullOrEmpty(notificationTypeCode))
                {
                    query = query.Where(n => n.NotificationTypeCode == notificationTypeCode);
                }
                
                var notifications = await query.ToListAsync();
                var count = notifications.Count;
                
                if (count == 0) return 0;
                
                var now = DateTime.UtcNow;
                
                // Batch update for performance
                // Let EF Core manage RowVersion automatically via [Timestamp] attribute
                // Each notification will get its own RowVersion update
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                }
                
                await _context.SaveChangesAsync();
                
                // Record read history for all
                foreach (var notification in notifications)
                {
                    await RecordReadHistoryAsync(
                        notification.Id, 
                        userId, 
                        deviceId, 
                        userAgent, 
                        "web", 
                        ipAddress, 
                        "bulk");
                }
                
                // Broadcast bulk read state change
                if (_hubContext != null)
                {
                    await _hubContext.Clients
                        .Group($"notifications_{userId}")
                        .SendAsync("BulkNotificationReadStateChanged", new
                        {
                            Count = count,
                            NotificationIds = notifications.Select(n => n.Id).ToList(),
                            ReadAt = now
                        });
                }
                
                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                throw;
            }
        }
        
        public async Task<bool> MarkAsUnreadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
                
                if (notification == null || !notification.IsRead) return false;
                
                // Let EF Core manage RowVersion automatically via [Timestamp] attribute
                notification.IsRead = false;
                notification.ReadAt = null;
                
                await _context.SaveChangesAsync();
                
                // Broadcast unread state change
                if (_hubContext != null)
                {
                    await _hubContext.Clients
                        .Group($"notifications_{userId}")
                        .SendAsync("NotificationReadStateChanged", new
                        {
                            NotificationId = notificationId,
                            IsRead = false,
                            ReadAt = (DateTime?)null
                        });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as unread", notificationId);
                throw;
            }
        }
        
        private async Task RecordReadHistoryAsync(
            Guid notificationId,
            Guid userId,
            string? deviceId,
            string? userAgent,
            string? channel,
            string? ipAddress,
            string readMethod)
        {
            try
            {
                var history = new NotificationReadHistory
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notificationId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow,
                    DeviceId = deviceId,
                    UserAgent = userAgent,
                    Channel = channel ?? "web",
                    IpAddress = ipAddress,
                    ReadMethod = readMethod
                };
                
                _context.NotificationReadHistories.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record read history for notification {NotificationId}", notificationId);
                // Don't throw - read history is non-critical
            }
        }
        
        public async Task<NotificationReadHistory?> GetLastReadHistoryAsync(Guid notificationId)
        {
            return await _context.NotificationReadHistories
                .Where(h => h.NotificationId == notificationId)
                .OrderByDescending(h => h.ReadAt)
                .FirstOrDefaultAsync();
        }
        
        public async Task<List<NotificationReadHistory>> GetReadHistoryAsync(Guid notificationId)
        {
            return await _context.NotificationReadHistories
                .Where(h => h.NotificationId == notificationId)
                .OrderByDescending(h => h.ReadAt)
                .ToListAsync();
        }
        
        public async Task<bool> IsReadAsync(Guid notificationId, Guid userId)
        {
            return await _context.UserNotifications
                .AnyAsync(n => n.Id == notificationId && n.UserId == userId && n.IsRead);
        }
        
        public async Task SyncReadStateAsync(Guid userId, List<Guid> readNotificationIds)
        {
            // Sync read state from another device
            // Used for mobile app sync
            try
            {
                var notifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId && readNotificationIds.Contains(n.Id) && !n.IsRead)
                    .ToListAsync();
                
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
                
                foreach (var notification in notifications)
                {
                    await MarkAsReadAsync(notification.Id, userId, null, userAgent, "sync");
                }
                
                _logger.LogInformation("Synced {Count} read states for user {UserId}", notifications.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing read state for user {UserId}", userId);
                throw;
            }
        }
    }
}

