using System.Text.Json;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;
using AmesaBackend.Auth.Data;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Admin.Services;

public interface IMessagingAdminService
{
    Task<IReadOnlyCollection<AdminNotificationDto>> GetRecentNotificationsAsync(int limit = 50);
    Task<SendAdminNotificationResult> QueueNotificationAsync(SendAdminNotificationRequest request);
}

public sealed class MessagingAdminService : IMessagingAdminService
{
    private readonly NotificationDbContext _notificationContext;
    private readonly AuthDbContext _authContext;
    private readonly IAdminPermissionService _permissions;
    private readonly IAdminAuditService _audit;
    private readonly ILogger<MessagingAdminService> _logger;

    public MessagingAdminService(
        NotificationDbContext notificationContext,
        AuthDbContext authContext,
        IAdminPermissionService permissions,
        IAdminAuditService audit,
        ILogger<MessagingAdminService> logger)
    {
        _notificationContext = notificationContext;
        _authContext = authContext;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<AdminNotificationDto>> GetRecentNotificationsAsync(int limit = 50)
    {
        await _permissions.RequirePermissionAsync(AdminPermissionNames.SettingsManage);

        var notifications = await _notificationContext.UserNotifications
            .AsNoTracking()
            .Where(n => !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync();

        var notificationIds = notifications.Select(n => n.Id).ToList();
        var userIds = notifications.Select(n => n.UserId).Distinct().ToList();

        var deliveryStats = await _notificationContext.NotificationDeliveries
            .AsNoTracking()
            .Where(d => notificationIds.Contains(d.NotificationId))
            .GroupBy(d => d.NotificationId)
            .Select(g => new
            {
                NotificationId = g.Key,
                Pending = g.Count(d => d.Status == NotificationChannelConstants.StatusPending),
                Sent = g.Count(d => d.Status == NotificationChannelConstants.StatusSent || d.Status == NotificationChannelConstants.StatusDelivered),
                Failed = g.Count(d => d.Status == NotificationChannelConstants.StatusFailed || d.Status == NotificationChannelConstants.StatusBounced)
            })
            .ToDictionaryAsync(x => x.NotificationId);

        var userEmails = await _authContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(x => x.Id, x => x.Email);

        return notifications.Select(n =>
        {
            deliveryStats.TryGetValue(n.Id, out var stats);
            return new AdminNotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                UserEmail = userEmails.GetValueOrDefault(n.UserId),
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                PendingDeliveries = stats?.Pending ?? 0,
                SentDeliveries = stats?.Sent ?? 0,
                FailedDeliveries = stats?.Failed ?? 0
            };
        }).ToList();
    }

    public async Task<SendAdminNotificationResult> QueueNotificationAsync(SendAdminNotificationRequest request)
    {
        await _permissions.RequirePermissionAsync(AdminPermissionNames.SettingsManage);

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Title and message are required.");
        }

        var user = await ResolveUserAsync(request.UserLookup);
        var channels = GetChannels(request);
        if (!channels.Any())
        {
            throw new InvalidOperationException("Select at least one delivery channel.");
        }

        var externalChannels = channels
            .Where(c => !string.Equals(c, "in_app", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var isQueuePlaceholder = externalChannels.Any();

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = string.IsNullOrWhiteSpace(request.Type) ? NotificationTypeConstants.SystemAnnouncement : request.Type.Trim(),
            NotificationTypeCode = string.IsNullOrWhiteSpace(request.Type) ? NotificationTypeConstants.SystemAnnouncement : request.Type.Trim(),
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Data = JsonSerializer.Serialize(new
            {
                source = "admin",
                queuedByAdminUserId = await _permissions.GetCurrentAdminUserIdAsync()
            }),
            IsDeleted = isQueuePlaceholder,
            DeletedAt = isQueuePlaceholder ? DateTime.UtcNow : null,
            DeletedBy = isQueuePlaceholder ? "admin_queue_placeholder" : null,
            CreatedAt = DateTime.UtcNow
        };

        _notificationContext.UserNotifications.Add(notification);

        foreach (var channel in externalChannels)
        {
            _notificationContext.NotificationQueue.Add(new NotificationQueue
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.Id,
                Channel = channel,
                Priority = 8,
                ScheduledFor = request.ScheduledFor,
                Status = NotificationChannelConstants.QueueStatusPending,
                MaxRetries = NotificationChannelConstants.DefaultMaxRetries,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _notificationContext.SaveChangesAsync();

        _logger.LogInformation("Admin queued notification {NotificationId} for user {UserId}", notification.Id, user.Id);
        await _audit.LogAsync("notification.queued", "notification", notification.Id, new
        {
            user.Id,
            user.Email,
            notification.Type,
            notification.Title,
            channels
        });

        return new SendAdminNotificationResult
        {
            NotificationId = notification.Id,
            UserId = user.Id,
            UserEmail = user.Email,
            QueuedChannels = channels
        };
    }

    private async Task<(Guid Id, string Email)> ResolveUserAsync(string userLookup)
    {
        if (string.IsNullOrWhiteSpace(userLookup))
        {
            throw new InvalidOperationException("User id or email is required.");
        }

        var lookup = userLookup.Trim();
        var query = _authContext.Users.AsNoTracking().Where(u => u.DeletedAt == null);

        var user = Guid.TryParse(lookup, out var userId)
            ? await query.Where(u => u.Id == userId).Select(u => new { u.Id, u.Email }).FirstOrDefaultAsync()
            : await query.Where(u => EF.Functions.ILike(u.Email, lookup)).Select(u => new { u.Id, u.Email }).FirstOrDefaultAsync();

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return (user.Id, user.Email);
    }

    private static IReadOnlyCollection<string> GetChannels(SendAdminNotificationRequest request)
    {
        var channels = new List<string>();
        if (request.InApp)
        {
            channels.Add("in_app");
        }

        if (request.Email)
        {
            channels.Add(NotificationChannelConstants.Email);
        }

        if (request.Sms)
        {
            channels.Add(NotificationChannelConstants.SMS);
        }

        if (request.Push)
        {
            channels.Add(NotificationChannelConstants.Push);
        }

        if (request.WebPush)
        {
            channels.Add(NotificationChannelConstants.WebPush);
        }

        if (request.Telegram)
        {
            channels.Add(NotificationChannelConstants.Telegram);
        }

        return channels.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
