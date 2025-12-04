using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AmesaBackend.Auth.Services;

public class SessionService : ISessionService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        AuthDbContext context,
        ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
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

                if (activeSessions.Count >= 5)
                {
                    // Remove oldest sessions (keep 4 most recent)
                    var sessionsToRemove = activeSessions.Take(activeSessions.Count - 4).ToList();
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
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionToken == sessionToken);

        if (session != null)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task LogoutAllDevicesAsync(Guid userId)
    {
        await InvalidateAllSessionsAsync(userId);
    }

    public async Task InvalidateAllSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }

    public string GenerateDeviceId(string userAgent, string ipAddress)
    {
        // Hash user agent + IP prefix for device fingerprinting
        var ipPrefix = ipAddress.Split('.').Take(3).Aggregate((a, b) => $"{a}.{b}");
        var input = $"{userAgent}|{ipPrefix}";
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
}






