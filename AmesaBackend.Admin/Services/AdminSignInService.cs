using System.Collections.Concurrent;
using System.Security.Cryptography;
using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace AmesaBackend.Admin.Services;

public interface IAdminSignInService
{
    Task<AdminSignInResult> PasswordSignInAsync(string email, string password);
    Task<AdminSignInResult> CompleteMfaSignInAsync(string code);
}

public sealed class AdminSignInService : IAdminSignInService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 30;
    private const int SessionTimeoutMinutes = 120;

    private static readonly ConcurrentDictionary<string, FailedLoginAttempt> FailedAttempts = new();

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly AdminDbContext? _adminDbContext;
    private readonly IAdminAuditService _audit;
    private readonly ILogger<AdminSignInService> _logger;

    private sealed class FailedLoginAttempt
    {
        public int Count { get; set; }
        public DateTime? LockedUntil { get; set; }
    }

    public AdminSignInService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IAdminAuditService audit,
        ILogger<AdminSignInService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _adminDbContext = serviceProvider.GetService<AdminDbContext>();
        _audit = audit;
        _logger = logger;
    }

    public async Task<AdminSignInResult> PasswordSignInAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (IsAccountLocked(normalizedEmail))
        {
            _logger.LogWarning("Admin login attempt for locked account: {Email}", normalizedEmail);
            return AdminSignInResult.Failed();
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            RecordFailedAttempt(normalizedEmail);
            return AdminSignInResult.Failed();
        }

        var adminUser = await FindAdminUserAsync(normalizedEmail);
        if (adminUser == null)
        {
            return await TryLegacySignInAsync(normalizedEmail, password);
        }

        if (string.IsNullOrWhiteSpace(adminUser.PasswordHash) || !adminUser.PasswordHash.StartsWith("$2"))
        {
            _logger.LogWarning("Admin user {Email} has an invalid password hash format", normalizedEmail);
            RecordFailedAttempt(normalizedEmail);
            return AdminSignInResult.Failed();
        }

        if (!BCrypt.Net.BCrypt.Verify(password.Trim(), adminUser.PasswordHash))
        {
            RecordFailedAttempt(normalizedEmail);
            await _audit.LogAsync("admin.sign_in.failed", "admin_user", adminUser.Id, new { reason = "invalid_password", email = normalizedEmail }, adminUser.Id);
            return AdminSignInResult.Failed();
        }

        ClearFailedAttempts(normalizedEmail);

        if (adminUser.TwoFactorEnabled)
        {
            await SetPendingMfaAsync(adminUser);
            await _audit.LogAsync("admin.sign_in.mfa_required", "admin_user", adminUser.Id, new { email = adminUser.Email }, adminUser.Id);
            return AdminSignInResult.MfaRequired();
        }

        return await SetAuthenticationSuccessAsync(adminUser.Email, adminUser.Id);
    }

    public async Task<AdminSignInResult> CompleteMfaSignInAsync(string code)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var pendingEmail = httpContext?.Session.GetString("PendingAdminEmail");
        var pendingUserId = httpContext?.Session.GetString("PendingAdminUserId");

        if (httpContext?.Session == null ||
            string.IsNullOrWhiteSpace(pendingEmail) ||
            !Guid.TryParse(pendingUserId, out var adminUserId))
        {
            return AdminSignInResult.Failed();
        }

        var adminUser = await _adminDbContext!.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == adminUserId && u.IsActive);

        if (adminUser == null || !adminUser.TwoFactorEnabled || string.IsNullOrWhiteSpace(adminUser.TwoFactorSecret))
        {
            ClearPendingMfa(httpContext);
            await _audit.LogAsync("admin.sign_in.mfa_failed", "admin_user", adminUserId, new { reason = "missing_mfa_state", email = pendingEmail }, adminUserId);
            return AdminSignInResult.Failed();
        }

        if (!VerifyTotp(adminUser.TwoFactorSecret, code))
        {
            _logger.LogWarning("Invalid MFA code for admin {Email}", pendingEmail);
            await _audit.LogAsync("admin.sign_in.mfa_failed", "admin_user", adminUser.Id, new { reason = "invalid_code", email = pendingEmail }, adminUser.Id);
            return AdminSignInResult.Failed();
        }

        adminUser.LastMfaAt = DateTime.UtcNow;
        await _adminDbContext.SaveChangesAsync();

        ClearPendingMfa(httpContext);
        return await SetAuthenticationSuccessAsync(adminUser.Email, adminUser.Id);
    }

    private async Task<AdminUser?> FindAdminUserAsync(string normalizedEmail)
    {
        if (_adminDbContext == null)
        {
            return null;
        }

        try
        {
            return await _adminDbContext.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, normalizedEmail) && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error during admin sign-in");
            RecordFailedAttempt(normalizedEmail);
            return null;
        }
    }

    private async Task<AdminSignInResult> TryLegacySignInAsync(string normalizedEmail, string password)
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
            ?? _configuration["AdminSettings:Email"];
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? _configuration["AdminSettings:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            RecordFailedAttempt(normalizedEmail);
            return AdminSignInResult.Failed();
        }

        var emailMatches = normalizedEmail == adminEmail.Trim().ToLowerInvariant();
        var passwordMatches = password.Trim() == adminPassword.Trim();
        if (!emailMatches || !passwordMatches)
        {
            RecordFailedAttempt(normalizedEmail);
            await _audit.LogAsync("admin.sign_in.failed", "admin_user", Guid.Empty, new { reason = "invalid_legacy_credentials", email = normalizedEmail });
            return AdminSignInResult.Failed();
        }

        ClearFailedAttempts(normalizedEmail);
        return await SetAuthenticationSuccessAsync(adminEmail.Trim(), null);
    }

    private async Task SetPendingMfaAsync(AdminUser adminUser)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available.");

        await EnsureSessionAvailableAsync(httpContext);
        httpContext.Session.SetString("PendingAdminEmail", adminUser.Email);
        httpContext.Session.SetString("PendingAdminUserId", adminUser.Id.ToString());
        httpContext.Session.SetString("PendingAdminLoginTime", DateTime.UtcNow.ToString("O"));
        await httpContext.Session.CommitAsync();
    }

    private async Task<AdminSignInResult> SetAuthenticationSuccessAsync(string email, Guid? adminUserId)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available.");

        await EnsureSessionAvailableAsync(httpContext);

        httpContext.Session.SetString("AdminEmail", email);
        httpContext.Session.SetString("AdminLoginTime", DateTime.UtcNow.ToString("O"));

        var sessionToken = GenerateSessionToken();
        httpContext.Session.SetString("AdminSessionToken", sessionToken);

        if (adminUserId.HasValue)
        {
            await RecordAdminSessionAsync(adminUserId.Value, sessionToken, httpContext);
        }

        await httpContext.Session.CommitAsync();
        _logger.LogInformation("Admin session established for {Email}", email);
        await _audit.LogAsync("admin.sign_in.succeeded", "admin_user", adminUserId ?? Guid.Empty, new { email }, adminUserId);
        return AdminSignInResult.Success();
    }

    private async Task RecordAdminSessionAsync(Guid adminUserId, string sessionToken, HttpContext httpContext)
    {
        if (_adminDbContext == null)
        {
            return;
        }

        try
        {
            _adminDbContext.AdminSessions.Add(new AdminSession
            {
                Id = Guid.NewGuid(),
                AdminUserId = adminUserId,
                SessionToken = sessionToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes),
                LastSeenAt = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault()
            });

            await _adminDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record admin session. Login will continue using ASP.NET session state.");
        }
    }

    private static bool VerifyTotp(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(1, 1));
    }

    private static async Task EnsureSessionAvailableAsync(HttpContext httpContext)
    {
        if (!httpContext.Session.IsAvailable)
        {
            await httpContext.Session.LoadAsync();
        }

        if (!httpContext.Session.IsAvailable)
        {
            throw new InvalidOperationException("Admin session storage is not available.");
        }
    }

    private static string GenerateSessionToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static void ClearPendingMfa(HttpContext httpContext)
    {
        httpContext.Session.Remove("PendingAdminEmail");
        httpContext.Session.Remove("PendingAdminUserId");
        httpContext.Session.Remove("PendingAdminLoginTime");
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(forwardedFor)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwardedFor.Split(',')[0].Trim();
    }

    private static bool IsAccountLocked(string normalizedEmail)
    {
        if (FailedAttempts.TryGetValue(normalizedEmail, out var attempt))
        {
            if (attempt.LockedUntil.HasValue && attempt.LockedUntil.Value > DateTime.UtcNow)
            {
                return true;
            }

            if (attempt.LockedUntil.HasValue && attempt.LockedUntil.Value <= DateTime.UtcNow)
            {
                FailedAttempts.TryRemove(normalizedEmail, out _);
            }
        }

        return false;
    }

    private void RecordFailedAttempt(string normalizedEmail)
    {
        FailedAttempts.AddOrUpdate(
            normalizedEmail,
            new FailedLoginAttempt { Count = 1 },
            (_, existing) =>
            {
                existing.Count++;
                if (existing.Count >= MaxFailedAttempts)
                {
                    existing.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
                    _logger.LogWarning("Admin account {Email} locked due to failed sign-in attempts", normalizedEmail);
                }

                return existing;
            });
    }

    private static void ClearFailedAttempts(string normalizedEmail)
    {
        FailedAttempts.TryRemove(normalizedEmail, out _);
    }
}
