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

        public async Task<object> GetDashboardAnalyticsAsync(Guid userId, int? page = null, int? limit = null)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId)
                .CountAsync();

            var activities = await _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .CountAsync();

            // Pagination defaults and limits
            const int defaultPage = 1;
            const int defaultLimit = 10;
            const int maxLimit = 100;
            
            var currentPage = page ?? defaultPage;
            var pageSize = limit ?? defaultLimit;
            
            // Enforce max page size for performance
            if (pageSize > maxLimit)
            {
                pageSize = maxLimit;
            }
            
            if (currentPage < 1)
            {
                currentPage = defaultPage;
            }
            
            var skip = (currentPage - 1) * pageSize;

            // Read-only query - use AsNoTracking for performance
            var recentActivitiesQuery = _context.UserActivityLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt);

            var totalActivitiesCount = await recentActivitiesQuery.CountAsync();

            var recentActivities = await recentActivitiesQuery
                .Skip(skip)
                .Take(pageSize)
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
                RecentActivities = recentActivities,
                Pagination = new
                {
                    Page = currentPage,
                    PageSize = pageSize,
                    TotalItems = totalActivitiesCount,
                    TotalPages = (int)Math.Ceiling(totalActivitiesCount / (double)pageSize),
                    HasNextPage = (currentPage * pageSize) < totalActivitiesCount,
                    HasPreviousPage = currentPage > 1
                }
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

