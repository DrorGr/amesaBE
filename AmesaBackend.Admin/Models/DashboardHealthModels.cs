namespace AmesaBackend.Admin.Models;

public sealed class DashboardHealthOverview
{
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<HealthCheckItem> Services { get; set; } = Array.Empty<HealthCheckItem>();
    public IReadOnlyList<HealthCheckItem> Products { get; set; } = Array.Empty<HealthCheckItem>();
    public PresenceSnapshot Presence { get; set; } = new();
}

public sealed class HealthCheckItem
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "service";
    public string Status { get; set; } = "unknown";
    public string? Detail { get; set; }
    public int? ResponseTimeMs { get; set; }
    public bool IsConfigured { get; set; } = true;
}

public sealed class PresenceSnapshot
{
    public int LoggedInUsers { get; set; }
    public int LoggedInSessions { get; set; }
    public int GuestSessions { get; set; }
    public int AdminSessionsOnline { get; set; }
    public int PresenceWindowMinutes { get; set; } = 15;
}
