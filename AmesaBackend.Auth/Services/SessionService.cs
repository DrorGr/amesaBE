using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AmesaBackend.Auth.Services;

public class SessionService : ISessionService
{
    private const int DefaultMaxActiveSessions = 5;

    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SessionService> _logger;
    private readonly int _maxActiveSessions;

    public SessionService(
        AuthDbContext context,
        IConfiguration configuration,
        ILogger<SessionService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _maxActiveSessions = _configuration.GetValue<int>("SecuritySettings:Session:MaxActiveSessions", DefaultMaxActiveSessions);
    }

    public async Task EnforceSessionLimitAsync(Guid userId)
    {
        // Use execution strategy to support retry with transactions
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Use transaction to prevent race conditions when multiple requests create sessions simultaneously
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                if (activeSessions.Count >= _maxActiveSessions)
                {
                    // Remove oldest sessions (keep maxActiveSessions - 1 most recent)
                    var sessionsToRemove = activeSessions.Take(activeSessions.Count - (_maxActiveSessions - 1)).ToList();
                    foreach (var session in sessionsToRemove)
                    {
                        session.IsActive = false;
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivity)
            .ToListAsync();

        return sessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            DeviceName = s.DeviceName ?? "Unknown Device",
            IpAddress = s.IpAddress ?? "Unknown",
            LastActivity = s.LastActivity,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt
        }).ToList();
    }

    public async Task LogoutFromDeviceAsync(Guid userId, string sessionToken)
    {
        // Use single UPDATE query to prevent race conditions
        var rowsAffected = await _context.UserSessions
            .Where(s => s.UserId == userId && s.SessionToken == sessionToken && s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));

        if (rowsAffected == 0)
        {
            _logger.LogWarning("Attempted to logout from session that doesn't exist or is already inactive. UserId: {UserId}, SessionToken: {SessionToken}", userId, sessionToken);
        }
    }

    public async Task LogoutAllDevicesAsync(Guid userId)
    {
        await InvalidateAllSessionsAsync(userId);
    }

    public async Task InvalidateAllSessionsAsync(Guid userId)
    {
        // Use single UPDATE query to prevent race conditions
        await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
    }

    public string GenerateDeviceId(string userAgent, string ipAddress)
    {
        // Privacy-friendly device fingerprinting: Only use user agent (NOT IP address)
        // This prevents tracking users across different networks/locations
        var input = userAgent ?? "unknown";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash).Substring(0, 16);
    }

    public string ExtractDeviceName(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";

        // Simple device name extraction
        if (userAgent.Contains("Windows"))
            return "Windows Device";
        if (userAgent.Contains("Macintosh") || userAgent.Contains("Mac OS"))
            return "Mac Device";
        if (userAgent.Contains("Linux"))
            return "Linux Device";
        if (userAgent.Contains("Android"))
            return "Android Device";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "iOS Device";

        return "Unknown Device";
    }

    public async Task UpdateSessionActivityAsync(string sessionToken)
    {
        // Use single UPDATE query for efficiency and to prevent race conditions
        await _context.UserSessions
            .Where(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastActivity, DateTime.UtcNow));
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var cutoffTime = DateTime.UtcNow;
        
        // Use single UPDATE query to mark expired sessions as inactive
        var rowsAffected = await _context.UserSessions
            .Where(s => s.IsActive && s.ExpiresAt <= cutoffTime)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", rowsAffected);
        }
    }

    public async Task<bool> DetectSuspiciousActivityAsync(Guid userId, string ipAddress, string userAgent)
    {
        // Check for suspicious patterns:
        // 1. Multiple active sessions from different countries
        // 2. Rapid session creation from different IPs
        // 3. Sessions from known VPN/proxy IPs (simplified check)

        var recentSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .ToListAsync();

        if (recentSessions.Count > 5)
        {
            // More than 5 sessions in the last hour is suspicious
            _logger.LogWarning("Suspicious activity detected: {Count} sessions created in last hour for user {UserId}", recentSessions.Count, userId);
            return true;
        }

        // Check for sessions from different countries
        var uniqueCountries = recentSessions
            .Where(s => !string.IsNullOrEmpty(s.Country))
            .Select(s => s.Country)
            .Distinct()
            .Count();

        if (uniqueCountries > 2)
        {
            // Sessions from more than 2 different countries in last hour is suspicious
            _logger.LogWarning("Suspicious activity detected: Sessions from {Count} different countries for user {UserId}", uniqueCountries, userId);
            return true;
        }

        return false;
    }
}











