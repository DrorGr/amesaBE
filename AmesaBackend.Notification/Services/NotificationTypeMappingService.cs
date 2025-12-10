using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Shared.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.Services
{
    public interface INotificationTypeMappingService
    {
        Task<string> GetFeatureAsync(string notificationTypeCode);
        Task<string> GetCategoryAsync(string notificationTypeCode);
        Task<bool> IsCriticalAsync(string notificationTypeCode);
        Task<List<string>> GetDefaultChannelsAsync(string notificationTypeCode);
        Task<bool> TypeExistsAsync(string notificationTypeCode);
        Task<NotificationType?> GetNotificationTypeAsync(string notificationTypeCode);
    }
    
    public class NotificationTypeMappingService : INotificationTypeMappingService
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationTypeMappingService> _logger;
        private readonly ICache _cache;
        
        public NotificationTypeMappingService(
            NotificationDbContext context,
            ILogger<NotificationTypeMappingService> logger,
            ICache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }
        
        public async Task<NotificationType?> GetNotificationTypeAsync(string notificationTypeCode)
        {
            var cacheKey = $"notification_type:{notificationTypeCode}";
            var cached = await _cache.GetRecordAsync<NotificationType>(cacheKey);
            if (cached != null) return cached;
            
            var type = await _context.NotificationTypes
                .FirstOrDefaultAsync(t => t.Code == notificationTypeCode);
            
            if (type != null)
            {
                await _cache.SetRecordAsync(cacheKey, type, TimeSpan.FromHours(24));
            }
            
            return type;
        }
        
        public async Task<string> GetFeatureAsync(string notificationTypeCode)
        {
            var cacheKey = $"notification_type:{notificationTypeCode}:feature";
            var cached = await _cache.GetRecordAsync<string>(cacheKey);
            if (cached != null) return cached;
            
            var type = await _context.NotificationTypes
                .FirstOrDefaultAsync(t => t.Code == notificationTypeCode);
            
            if (type == null)
            {
                _logger.LogWarning("Notification type not found: {TypeCode}", notificationTypeCode);
                return "unknown";
            }
            
            await _cache.SetRecordAsync(cacheKey, type.Feature, TimeSpan.FromHours(24));
            return type.Feature;
        }
        
        public async Task<string> GetCategoryAsync(string notificationTypeCode)
        {
            var cacheKey = $"notification_type:{notificationTypeCode}:category";
            var cached = await _cache.GetRecordAsync<string>(cacheKey);
            if (cached != null) return cached;
            
            var type = await _context.NotificationTypes
                .FirstOrDefaultAsync(t => t.Code == notificationTypeCode);
            
            if (type == null)
            {
                _logger.LogWarning("Notification type not found: {TypeCode}", notificationTypeCode);
                return "unknown";
            }
            
            await _cache.SetRecordAsync(cacheKey, type.Category, TimeSpan.FromHours(24));
            return type.Category;
        }
        
        public async Task<bool> IsCriticalAsync(string notificationTypeCode)
        {
            var cacheKey = $"notification_type:{notificationTypeCode}:critical";
            var cached = await _cache.GetRecordAsync<bool?>(cacheKey);
            if (cached.HasValue) return cached.Value;
            
            var type = await _context.NotificationTypes
                .FirstOrDefaultAsync(t => t.Code == notificationTypeCode);
            
            if (type == null)
            {
                _logger.LogWarning("Notification type not found: {TypeCode}", notificationTypeCode);
                return false;
            }
            
            await _cache.SetRecordAsync(cacheKey, type.IsCritical, TimeSpan.FromHours(24));
            return type.IsCritical;
        }
        
        public async Task<List<string>> GetDefaultChannelsAsync(string notificationTypeCode)
        {
            var cacheKey = $"notification_type:{notificationTypeCode}:channels";
            var cached = await _cache.GetRecordAsync<List<string>>(cacheKey);
            if (cached != null) return cached;
            
            var type = await _context.NotificationTypes
                .FirstOrDefaultAsync(t => t.Code == notificationTypeCode);
            
            if (type == null)
            {
                _logger.LogWarning("Notification type not found: {TypeCode}", notificationTypeCode);
                return new List<string> { "email" };
            }
            
            var channels = type.DefaultChannels?.ToList() ?? new List<string>();
            await _cache.SetRecordAsync(cacheKey, channels, TimeSpan.FromHours(24));
            return channels;
        }
        
        public async Task<bool> TypeExistsAsync(string notificationTypeCode)
        {
            var cacheKey = $"notification_type:{notificationTypeCode}:exists";
            var cached = await _cache.GetRecordAsync<bool?>(cacheKey);
            if (cached.HasValue) return cached.Value;
            
            var exists = await _context.NotificationTypes
                .AnyAsync(t => t.Code == notificationTypeCode);
            
            await _cache.SetRecordAsync(cacheKey, exists, TimeSpan.FromHours(24));
            return exists;
        }
    }
}

