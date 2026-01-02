using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Authentication;
using User = AmesaBackend.Auth.Models.User;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AmesaBackend.Auth.Services;

public class TokenService : ITokenService
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IJwtTokenManager _jwtTokenManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionService _sessionService;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        AuthDbContext context,
        IConfiguration configuration,
        IJwtTokenManager jwtTokenManager,
        IHttpContextAccessor httpContextAccessor,
        ISessionService sessionService,
        ILogger<TokenService> logger)
    {
        _context = context;
        _configuration = configuration;
        _jwtTokenManager = jwtTokenManager;
        _httpContextAccessor = httpContextAccessor;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(User user, bool rememberMe = false)
    {
        // Create refresh token first (needed for session token claim)
        var refreshToken = GenerateSecureToken();
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim("session_token", refreshToken) // Add session token to JWT for activity tracking
        };

        // Access token expiration (same for both remember me and session-based)
        var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryInMinutes", 60));
        var accessToken = _jwtTokenManager.GenerateAccessToken(claims, expiresAt);
        // RememberMe: 30 days (configurable), Session-based: 7 days (default)
        var refreshTokenExpiryDays = rememberMe 
            ? _configuration.GetValue<int>("JwtSettings:RememberMeExpiryInDays", 30)
            : _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryInDays", 7);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        // Extract IP and device info - use middleware values if available
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Items["ClientIp"]?.ToString() 
            ?? httpContext?.Connection.RemoteIpAddress?.ToString() 
            ?? httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? "unknown";
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
        var deviceId = httpContext?.Items["DeviceId"]?.ToString() ?? _sessionService.GenerateDeviceId(userAgent, ipAddress);
        var deviceName = _sessionService.ExtractDeviceName(userAgent);

        // Save session
        var session = new UserSession
        {
            UserId = user.Id,
            SessionToken = refreshToken,
            ExpiresAt = refreshExpiresAt,
            IsActive = true,
            RememberMe = rememberMe, // Store Remember Me status
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId,
            DeviceName = deviceName,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        _context.UserSessions.Add(session);

        // Limit to 5 active sessions
        await _sessionService.EnforceSessionLimitAsync(user.Id);

        await _context.SaveChangesAsync();

        return (accessToken, refreshToken, expiresAt);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // TODO: Implement GetClaimsFromExpiredToken method in IJwtTokenManager
            // var claims = _jwtTokenManager.GetClaimsFromExpiredToken(token);
            // return claims != null && claims.Any();
            return false; // Temporary fix - always return false for expired tokens
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure token using URL-safe Base64 encoding.
    /// URL-safe encoding replaces '+' with '-', '/' with '_', and removes padding '='.
    /// This ensures tokens can be safely used in URLs without encoding.
    /// </summary>
    public string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        
        // Convert to Base64 and make it URL-safe
        var base64 = Convert.ToBase64String(bytes);
        // Replace '+' with '-', '/' with '_', and remove padding '='
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}

