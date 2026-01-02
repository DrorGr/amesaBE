using Microsoft.EntityFrameworkCore;
using AmesaBackend.Analytics.Data;
using AmesaBackend.Analytics.Models;
using AmesaBackend.Analytics.DTOs;
using AmesaBackend.Analytics.Services.Interfaces;
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

        public async Task<List<UserSessionDto>> GetUserSessionsAsync(Guid userId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.UserSessions
                .AsNoTracking()
                .Include(s => s.ActivityLogs)
                .Where(s => s.UserId == userId);

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt <= toDate.Value);
            }

            var sessions = await query
                .OrderByDescending(s => s.LastActivity)
                .Take(500)
                .Select(s => new UserSessionDto
                {
                    Id = s.Id,
                    StartTime = s.CreatedAt,
                    EndTime = s.LastActivity,
                    DurationSeconds = (s.LastActivity - s.CreatedAt).TotalSeconds,
                    PageViews = 0,
                    Events = s.ActivityLogs.Count,
                    Device = s.DeviceType ?? string.Empty,
                    Browser = s.Browser ?? string.Empty,
                    IpAddress = null // redact PII
                })
                .ToListAsync();

            return sessions;
        }

        public async Task<UserSessionDto?> GetUserSessionAsync(Guid userId, Guid sessionId)
        {
            var session = await _context.UserSessions
                .AsNoTracking()
                .Include(s => s.ActivityLogs)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == sessionId);

            if (session == null) return null;

            return new UserSessionDto
            {
                Id = session.Id,
                StartTime = session.CreatedAt,
                EndTime = session.LastActivity,
                DurationSeconds = (session.LastActivity - session.CreatedAt).TotalSeconds,
                PageViews = 0,
                Events = session.ActivityLogs.Count,
                Device = session.DeviceType ?? string.Empty,
                Browser = session.Browser ?? string.Empty,
                IpAddress = null // redact
            };
        }

        public async Task<List<ActivityLogDto>> GetActivityAsync(Guid userId, ActivityFilters filters)
        {
            var query = _context.UserActivityLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId);

            if (filters.FromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filters.FromDate.Value);
            }
            if (filters.ToDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= filters.ToDate.Value);
            }

            var limit = filters.Limit.HasValue && filters.Limit.Value > 0 && filters.Limit.Value <= 500
                ? filters.Limit.Value
                : 100;

            var rawItems = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.ResourceType,
                    a.Action,
                    a.CreatedAt,
                    a.Details
                })
                .ToListAsync();

            var items = rawItems.Select(a => new ActivityLogDto
            {
                Id = a.Id,
                EventType = a.ResourceType ?? "activity",
                EventName = a.Action,
                Timestamp = a.CreatedAt,
                Page = null,
                Properties = a.Details != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(a.Details) : null
            }).ToList();

            return items;
        }
    }
}

