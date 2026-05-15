using System.Diagnostics;
using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Models;
using AmesaBackend.Admin.Security;
using AmesaBackend.Analytics.Data;
using AmesaBackend.Auth.Data;
using AmesaBackend.Content.Data;
using AmesaBackend.Lottery.Data;
using AmesaBackend.LotteryResults.Data;
using AmesaBackend.Notification.Data;
using AmesaBackend.Payment.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AmesaBackend.Admin.Services;

public interface IDashboardHealthService
{
    Task<DashboardHealthOverview> GetHealthOverviewAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardHealthService : IDashboardHealthService
{
    private const string StatusHealthy = "healthy";
    private const string StatusDegraded = "degraded";
    private const string StatusUnhealthy = "unhealthy";
    private const string StatusUnknown = "unknown";
    private const string StatusSkipped = "skipped";

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAdminPermissionService _permissions;
    private readonly AuthDbContext _authContext;
    private readonly AdminDbContext? _adminContext;
    private readonly AnalyticsDbContext? _analyticsContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<DashboardHealthService> _logger;

    public DashboardHealthService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IAdminPermissionService permissions,
        AuthDbContext authContext,
        IServiceProvider serviceProvider,
        ILogger<DashboardHealthService> logger,
        AdminDbContext? adminContext = null,
        AnalyticsDbContext? analyticsContext = null,
        IDistributedCache? distributedCache = null)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _permissions = permissions;
        _authContext = authContext;
        _adminContext = adminContext;
        _analyticsContext = analyticsContext;
        _serviceProvider = serviceProvider;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<DashboardHealthOverview> GetHealthOverviewAsync(CancellationToken cancellationToken = default)
    {
        await _permissions.RequirePermissionAsync(AdminPermissionNames.DashboardRead);

        var timeoutSeconds = _configuration.GetValue("ServiceHealth:ProbeTimeoutSeconds", 5);
        var presenceWindowMinutes = _configuration.GetValue("ServiceHealth:PresenceWindowMinutes", 15);
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 2, 30));

        var httpTargets = GetHttpTargets();
        var dbTargets = GetDatabaseTargets();

        var httpResults = await Task.WhenAll(httpTargets.Select(t => ProbeHttpAsync(t, timeout, cancellationToken)));
        var dbResults = await Task.WhenAll(dbTargets.Select(t => ProbeDatabaseAsync(t, cancellationToken)));
        var presence = await GetPresenceAsync(presenceWindowMinutes, cancellationToken);
        var redis = await ProbeRedisAsync(cancellationToken);

        var services = httpResults
            .Where(r => string.Equals(r.Category, "service", StringComparison.OrdinalIgnoreCase))
            .Concat(dbResults)
            .Append(redis)
            .OrderBy(r => r.Name)
            .ToList();

        var products = httpResults
            .Where(r => string.Equals(r.Category, "product", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Name)
            .ToList();

        return new DashboardHealthOverview
        {
            CheckedAtUtc = DateTime.UtcNow,
            Services = services,
            Products = products,
            Presence = presence
        };
    }

    private List<HealthProbeTarget> GetHttpTargets()
    {
        var targets = new List<HealthProbeTarget>();
        var section = _configuration.GetSection("ServiceHealth:Endpoints");
        if (section.Exists())
        {
            foreach (var child in section.GetChildren())
            {
                var url = child["Url"];
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                targets.Add(new HealthProbeTarget(
                    child.Key,
                    child["Name"] ?? child.Key,
                    child["Category"] ?? "service",
                    url.Trim(),
                    child["HealthPath"] ?? "/health"));
            }

            return targets;
        }

        foreach (var (key, name, category, defaultUrl) in DefaultHttpEndpoints)
        {
            var url = _configuration[$"Services:{key}:Url"] ?? defaultUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            targets.Add(new HealthProbeTarget(key, name, category, url.Trim(), "/health"));
        }

        return targets;
    }

    private static List<DbProbeTarget> GetDatabaseTargets() =>
    [
        new("db-auth", "Auth Database", typeof(AuthDbContext)),
        new("db-lottery", "Lottery Database", typeof(LotteryDbContext)),
        new("db-payment", "Payment Database", typeof(PaymentDbContext)),
        new("db-notification", "Notification Database", typeof(NotificationDbContext)),
        new("db-content", "Content Database", typeof(ContentDbContext)),
        new("db-lottery-results", "Lottery Results Database", typeof(LotteryResultsDbContext)),
        new("db-admin", "Admin Database", typeof(AdminDbContext)),
        new("db-analytics", "Analytics Database", typeof(AnalyticsDbContext))
    ];

    private async Task<HealthCheckItem> ProbeHttpAsync(HealthProbeTarget target, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target.BaseUrl))
        {
            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = target.Category,
                Status = StatusSkipped,
                Detail = "URL not configured",
                IsConfigured = false
            };
        }

        var healthUrl = BuildHealthUrl(target.BaseUrl, target.HealthPath ?? "/health");
        var client = _httpClientFactory.CreateClient("DashboardHealth");
        client.Timeout = timeout;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(healthUrl, cancellationToken);
            stopwatch.Stop();

            var status = response.IsSuccessStatusCode ? StatusHealthy : StatusUnhealthy;
            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = target.Category,
                Status = status,
                Detail = $"HTTP {(int)response.StatusCode}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = target.Category,
                Status = StatusUnhealthy,
                Detail = "Timeout",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDebug(ex, "Health probe failed for {Name} at {Url}", target.Name, healthUrl);
            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = target.Category,
                Status = StatusUnhealthy,
                Detail = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
    }

    private async Task<HealthCheckItem> ProbeDatabaseAsync(DbProbeTarget target, CancellationToken cancellationToken)
    {
        var context = _serviceProvider.GetService(target.DbContextType) as DbContext;
        if (context == null)
        {
            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = "service",
                Status = StatusSkipped,
                Detail = "Not registered",
                IsConfigured = false
            };
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = "service",
                Status = canConnect ? StatusHealthy : StatusUnhealthy,
                Detail = canConnect ? "Connected" : "Cannot connect",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDebug(ex, "Database health probe failed for {Name}", target.Name);
            return new HealthCheckItem
            {
                Key = target.Key,
                Name = target.Name,
                Category = "service",
                Status = StatusUnhealthy,
                Detail = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
    }

    private async Task<HealthCheckItem> ProbeRedisAsync(CancellationToken cancellationToken)
    {
        if (_distributedCache == null)
        {
            return new HealthCheckItem
            {
                Key = "redis",
                Name = "Redis Cache",
                Category = "service",
                Status = StatusSkipped,
                Detail = "Cache not configured",
                IsConfigured = false
            };
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var probeKey = $"admin:health:{Guid.NewGuid():N}";
            await _distributedCache.SetStringAsync(probeKey, "1", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            }, cancellationToken);
            var value = await _distributedCache.GetStringAsync(probeKey, cancellationToken);
            await _distributedCache.RemoveAsync(probeKey, cancellationToken);
            stopwatch.Stop();

            var healthy = value == "1";
            return new HealthCheckItem
            {
                Key = "redis",
                Name = "Redis Cache",
                Category = "service",
                Status = healthy ? StatusHealthy : StatusDegraded,
                Detail = healthy ? "Read/write OK" : "Unexpected read result",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckItem
            {
                Key = "redis",
                Name = "Redis Cache",
                Category = "service",
                Status = StatusUnhealthy,
                Detail = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                IsConfigured = true
            };
        }
    }

    private async Task<PresenceSnapshot> GetPresenceAsync(int presenceWindowMinutes, CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-Math.Clamp(presenceWindowMinutes, 1, 120));
        var now = DateTime.UtcNow;
        var snapshot = new PresenceSnapshot { PresenceWindowMinutes = presenceWindowMinutes };

        try
        {
            var activeSessionQuery = _authContext.UserSessions.AsNoTracking()
                .Where(s => s.IsActive && s.ExpiresAt > now && s.LastActivity >= windowStart);

            snapshot.LoggedInUsers = await activeSessionQuery
                .Where(s => s.UserId != null && s.UserId != Guid.Empty)
                .Select(s => s.UserId!.Value)
                .Distinct()
                .CountAsync(cancellationToken);

            snapshot.LoggedInSessions = await activeSessionQuery
                .Where(s => s.UserId != null && s.UserId != Guid.Empty)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load logged-in presence from auth sessions");
        }

        try
        {
            if (_analyticsContext != null)
            {
                snapshot.GuestSessions = await _analyticsContext.UserSessions.AsNoTracking()
                    .Where(s => s.IsActive && s.ExpiresAt > now && s.UserId == null && s.LastActivity >= windowStart)
                    .CountAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Analytics guest session count unavailable");
        }

        if (snapshot.GuestSessions == 0)
        {
            try
            {
                snapshot.GuestSessions = await _authContext.UserActivityLogs.AsNoTracking()
                    .Where(a => a.UserId == null && a.CreatedAt >= windowStart)
                    .Select(a => a.SessionId)
                    .Where(id => id != null)
                    .Distinct()
                    .CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Guest activity fallback unavailable");
            }
        }

        if (snapshot.GuestSessions == 0)
        {
            try
            {
                snapshot.GuestSessions = await _authContext.UserSessions.AsNoTracking()
                    .Where(s => s.IsActive && s.ExpiresAt > now && s.UserId == null && s.LastActivity >= windowStart)
                    .CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Auth anonymous session count unavailable");
            }
        }

        try
        {
            if (_adminContext != null)
            {
                snapshot.AdminSessionsOnline = await _adminContext.AdminSessions.AsNoTracking()
                    .Where(s => s.RevokedAt == null && s.ExpiresAt > now &&
                                (s.LastSeenAt == null || s.LastSeenAt >= windowStart))
                    .CountAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Admin session presence unavailable");
        }

        return snapshot;
    }

    private static string BuildHealthUrl(string baseUrl, string healthPath)
    {
        var trimmed = baseUrl.TrimEnd('/');
        var path = healthPath.StartsWith('/') ? healthPath : $"/{healthPath}";
        return $"{trimmed}{path}";
    }

    private static readonly (string key, string name, string category, string defaultUrl)[] DefaultHttpEndpoints =
    [
        ("AuthService", "Auth API", "service", "http://amesa-auth-service:8080"),
        ("LotteryService", "Lottery API", "service", "http://amesa-lottery-service:8080"),
        ("PaymentService", "Payment API", "service", "http://amesa-payment-service:8080"),
        ("NotificationService", "Notification API", "service", "http://amesa-notification-service:8080"),
        ("ContentService", "Content API", "service", "http://amesa-content-service:8080"),
        ("LotteryResultsService", "Lottery Results API", "service", "http://amesa-lottery-results-service:8080"),
        ("AnalyticsService", "Analytics API", "service", "http://amesa-analytics-service:8080"),
        ("AdminService", "Admin Panel", "service", "http://amesa-admin-service:8080"),
        ("Frontend", "Customer Web App", "product", "")
    ];

    private sealed record HealthProbeTarget(string Key, string Name, string Category, string BaseUrl, string? HealthPath);

    private sealed record DbProbeTarget(string Key, string Name, Type DbContextType);
}
