using System.Text.Json;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Admin.Services;

public interface IMessagingAdminService
{
    Task<IReadOnlyCollection<AdminNotificationDto>> GetRecentNotificationsAsync(int limit = 50);
    Task<AdminMessageRecipientPreviewDto> PreviewRecipientsAsync(SendAdminNotificationRequest request);
    Task<SendAdminNotificationResult> QueueNotificationAsync(SendAdminNotificationRequest request);
}

public sealed class MessagingAdminService : IMessagingAdminService
{
    private const int MaxGroupRecipientsPerSend = 500;

    private readonly NotificationDbContext _notificationContext;
    private readonly AuthDbContext _authContext;
    private readonly LotteryDbContext _lotteryContext;
    private readonly IAdminPermissionService _permissions;
    private readonly IAdminAuditService _audit;
    private readonly ILogger<MessagingAdminService> _logger;

    public MessagingAdminService(
        NotificationDbContext notificationContext,
        AuthDbContext authContext,
        LotteryDbContext lotteryContext,
        IAdminPermissionService permissions,
        IAdminAuditService audit,
        ILogger<MessagingAdminService> logger)
    {
        _notificationContext = notificationContext;
        _authContext = authContext;
        _lotteryContext = lotteryContext;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<AdminNotificationDto>> GetRecentNotificationsAsync(int limit = 50)
    {
        await RequireNotificationReadAccessAsync();

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

    public async Task<AdminMessageRecipientPreviewDto> PreviewRecipientsAsync(SendAdminNotificationRequest request)
    {
        await RequireNotificationSendAccessAsync();

        var recipients = await ResolveRecipientsAsync(request);
        return new AdminMessageRecipientPreviewDto
        {
            TotalCount = recipients.Count,
            Recipients = recipients
                .Take(25)
                .Select(r => new AdminMessageRecipientDto { UserId = r.Id, Email = r.Email })
                .ToList()
        };
    }

    public async Task<SendAdminNotificationResult> QueueNotificationAsync(SendAdminNotificationRequest request)
    {
        await RequireNotificationSendAccessAsync();

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Title and message are required.");
        }

        var recipients = await ResolveRecipientsAsync(request);
        var channels = GetChannels(request);
        if (!channels.Any())
        {
            throw new InvalidOperationException("Select at least one delivery channel.");
        }

        var externalChannels = channels
            .Where(c => !string.Equals(c, "in_app", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var includesInApp = request.InApp;
        var isQueuePlaceholder = externalChannels.Any() && !includesInApp;

        var queuedByAdminUserId = await _permissions.GetCurrentAdminUserIdAsync();
        var notifications = new List<UserNotification>();

        foreach (var user in recipients)
        {
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
                    targetMode = request.TargetMode,
                    groupBasis = request.GroupBasis,
                    queuedByAdminUserId
                }),
                IsDeleted = isQueuePlaceholder,
                DeletedAt = isQueuePlaceholder ? DateTime.UtcNow : null,
                DeletedBy = isQueuePlaceholder ? "admin_queue_placeholder" : null,
                CreatedAt = DateTime.UtcNow
            };

            notifications.Add(notification);
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
        }

        await _notificationContext.SaveChangesAsync();

        var firstNotification = notifications.First();
        var firstRecipient = recipients.First();

        _logger.LogInformation("Admin queued notification batch {NotificationId} for {RecipientCount} recipients", firstNotification.Id, recipients.Count);
        await _audit.LogAsync("notification.queued", "notification", firstNotification.Id, new
        {
            firstRecipient.Id,
            firstRecipient.Email,
            firstNotification.Type,
            firstNotification.Title,
            request.TargetMode,
            request.GroupBasis,
            RecipientCount = recipients.Count,
            channels
        });

        return new SendAdminNotificationResult
        {
            NotificationId = firstNotification.Id,
            UserId = firstRecipient.Id,
            UserEmail = firstRecipient.Email,
            RecipientCount = recipients.Count,
            QueuedChannels = channels
        };
    }

    private async Task<IReadOnlyCollection<(Guid Id, string Email)>> ResolveRecipientsAsync(SendAdminNotificationRequest request)
    {
        if (!string.Equals(request.TargetMode, "group", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { await ResolveUserAsync(request.UserLookup) };
        }

        var userIds = await ResolveGroupUserIdsAsync(request);
        if (!userIds.Any())
        {
            throw new InvalidOperationException("No users matched the selected group target.");
        }

        var recipients = await _authContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.Status == UserStatus.Active)
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        if (recipients.Count > MaxGroupRecipientsPerSend)
        {
            throw new InvalidOperationException($"Group target matched more than {MaxGroupRecipientsPerSend} active users. Narrow the target before sending.");
        }

        return recipients.Select(u => (u.Id, u.Email)).ToList();
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveGroupUserIdsAsync(SendAdminNotificationRequest request)
    {
        var basis = (request.GroupBasis ?? string.Empty).Trim().ToLowerInvariant();
        switch (basis)
        {
            case "house":
                if (!request.HouseId.HasValue)
                {
                    throw new InvalidOperationException("Select a house for house-based targeting.");
                }

                return await _lotteryContext.LotteryTickets
                    .AsNoTracking()
                    .Where(t => t.HouseId == request.HouseId.Value && t.UserId != Guid.Empty)
                    .Select(t => t.UserId)
                    .Distinct()
                    .ToListAsync();

            case "lottery":
                if (!request.LotteryHouseId.HasValue)
                {
                    throw new InvalidOperationException("Select a lottery house for lottery-based targeting.");
                }

                return await _lotteryContext.LotteryTickets
                    .AsNoTracking()
                    .Where(t => t.HouseId == request.LotteryHouseId.Value && t.UserId != Guid.Empty)
                    .Select(t => t.UserId)
                    .Distinct()
                    .ToListAsync();

            case "birthday":
                if (!request.BirthdayMonth.HasValue)
                {
                    throw new InvalidOperationException("Select a birthday month.");
                }
                if (request.BirthdayMonth is < 1 or > 12)
                {
                    throw new InvalidOperationException("Birthday month must be between 1 and 12.");
                }
                if (request.BirthdayDay is < 1 or > 31)
                {
                    throw new InvalidOperationException("Birthday day must be between 1 and 31.");
                }

                var birthdayQuery = _authContext.Users
                    .AsNoTracking()
                    .Where(u => u.DateOfBirth.HasValue && u.DateOfBirth.Value.Month == request.BirthdayMonth.Value);

                if (request.BirthdayDay.HasValue)
                {
                    birthdayQuery = birthdayQuery.Where(u => u.DateOfBirth!.Value.Day == request.BirthdayDay.Value);
                }

                return await birthdayQuery.Select(u => u.Id).Distinct().ToListAsync();

            case "location":
                if (string.IsNullOrWhiteSpace(request.LocationQuery))
                {
                    throw new InvalidOperationException("Enter a city or country for location targeting.");
                }

                var location = request.LocationQuery.Trim();
                return await _authContext.UserAddresses
                    .AsNoTracking()
                    .Where(a =>
                        (a.City != null && EF.Functions.ILike(a.City, $"%{location}%")) ||
                        (a.Country != null && EF.Functions.ILike(a.Country, $"%{location}%")))
                    .Select(a => a.UserId)
                    .Distinct()
                    .ToListAsync();

            case "language":
                if (string.IsNullOrWhiteSpace(request.Language))
                {
                    throw new InvalidOperationException("Enter a language code for language targeting.");
                }

                var language = request.Language.Trim().ToLowerInvariant();
                return await _authContext.Users
                    .AsNoTracking()
                    .Where(u => u.PreferredLanguage.ToLower() == language)
                    .Select(u => u.Id)
                    .Distinct()
                    .ToListAsync();

            default:
                throw new InvalidOperationException("Unsupported group targeting basis.");
        }
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

    private async Task RequireNotificationReadAccessAsync()
    {
        if (await _permissions.HasPermissionAsync(AdminPermissionNames.NotificationsRead) ||
            await _permissions.HasPermissionAsync(AdminPermissionNames.NotificationsSend) ||
            await _permissions.HasPermissionAsync(AdminPermissionNames.SettingsManage) ||
            await _permissions.HasPermissionAsync(AdminPermissionNames.AuditRead))
        {
            return;
        }

        throw new UnauthorizedAccessException($"Admin permission required: {AdminPermissionNames.NotificationsRead} or {AdminPermissionNames.SettingsManage}");
    }

    private async Task RequireNotificationSendAccessAsync()
    {
        if (await _permissions.HasPermissionAsync(AdminPermissionNames.NotificationsSend) ||
            await _permissions.HasPermissionAsync(AdminPermissionNames.SettingsManage))
        {
            return;
        }

        throw new UnauthorizedAccessException($"Admin permission required: {AdminPermissionNames.NotificationsSend} or {AdminPermissionNames.SettingsManage}");
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
