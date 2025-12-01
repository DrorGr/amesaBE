using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
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

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60"));
        var accessToken = _jwtTokenManager.GenerateAccessToken(claims, expiresAt);

        // Create refresh token
        var refreshToken = GenerateSecureToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"] ?? "7"));

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

    public string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

