using Microsoft.EntityFrameworkCore;
using AmesaBackend.Analytics.Data;
using AmesaBackend.Analytics.Models;
using System.Text.Json;

namespace AmesaBackend.Analytics.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AnalyticsDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(AnalyticsDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<object> GetDashboardAnalyticsAsync(Guid userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId)
                .CountAsync();

            var activities = await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .CountAsync();

            var recentActivities = await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new
                {
                    a.Action,
                    a.ResourceType,
                    a.CreatedAt
                })
                .ToListAsync();

            return new
            {
                TotalSessions = sessions,
                TotalActivities = activities,
                RecentActivities = recentActivities
            };
        }

        public async Task<object> GetLotteryStatsAsync(Guid? houseId)
        {
            // In a real implementation, this would aggregate data from Lottery Service
            return new
            {
                Message = "Lottery stats aggregation - requires integration with Lottery Service"
            };
        }

        public async Task LogActivityAsync(Guid? userId, Guid? sessionId, string action, string? resourceType = null, Guid? resourceId = null, object? details = null)
        {
            try
            {
                var log = new UserActivityLog
                {
                    UserId = userId,
                    SessionId = sessionId,
                    Action = action,
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Details = details != null ? JsonSerializer.Serialize(details) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging activity");
            }
        }
    }
}

