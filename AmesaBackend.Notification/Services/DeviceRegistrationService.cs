using Microsoft.EntityFrameworkCore;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.Services
{
    public class DeviceRegistrationService : IDeviceRegistrationService
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<DeviceRegistrationService> _logger;

        public DeviceRegistrationService(
            NotificationDbContext context,
            ILogger<DeviceRegistrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DeviceRegistration> RegisterDeviceAsync(
            Guid userId, 
            string deviceToken, 
            string platform, 
            string? deviceId = null, 
            string? deviceName = null, 
            string? appVersion = null)
        {
            // Check if device already registered
            var existing = await _context.DeviceRegistrations
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

            if (existing != null)
            {
                // Update existing registration
                existing.Platform = platform;
                existing.DeviceId = deviceId ?? existing.DeviceId;
                existing.DeviceName = deviceName ?? existing.DeviceName;
                existing.AppVersion = appVersion ?? existing.AppVersion;
                existing.IsActive = true;
                existing.LastUsedAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated device registration for user {UserId}, token {Token}", userId, deviceToken);
                return existing;
            }

            // Create new registration
            var registration = new DeviceRegistration
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceToken = deviceToken,
                Platform = platform,
                DeviceId = deviceId,
                DeviceName = deviceName,
                AppVersion = appVersion,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            _context.DeviceRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registered new device for user {UserId}, platform {Platform}", userId, platform);
            return registration;
        }

        public async Task<bool> UnregisterDeviceAsync(Guid userId, string deviceToken)
        {
            var device = await _context.DeviceRegistrations
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

            if (device == null)
            {
                return false;
            }

            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Unregistered device for user {UserId}, token {Token}", userId, deviceToken);
            return true;
        }

        public async Task<List<DeviceRegistration>> GetUserDevicesAsync(Guid userId)
        {
            return await _context.DeviceRegistrations
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastUsedAt)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserDeviceTokensAsync(Guid userId, string? platform = null)
        {
            var query = _context.DeviceRegistrations
                .Where(d => d.UserId == userId && d.IsActive);

            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(d => d.Platform == platform);
            }

            return await query
                .Select(d => d.DeviceToken)
                .ToListAsync();
        }

        public async Task<bool> UpdateDeviceLastUsedAsync(Guid userId, string deviceToken)
        {
            var device = await _context.DeviceRegistrations
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == deviceToken);

            if (device == null)
            {
                return false;
            }

            device.LastUsedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}







