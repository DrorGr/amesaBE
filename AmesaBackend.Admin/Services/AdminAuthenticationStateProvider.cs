using System.Security.Claims;
using AmesaBackend.Admin.Security;
using AmesaBackend.Auth.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace AmesaBackend.Admin.Services;

public sealed class AdminAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private readonly IAdminAuthService _authService;
    private readonly IAdminPermissionService _permissionService;
    private readonly ILogger<AdminAuthenticationStateProvider> _logger;

    public AdminAuthenticationStateProvider(
        IAdminAuthService authService,
        IAdminPermissionService permissionService,
        ILogger<AdminAuthenticationStateProvider> logger)
    {
        _authService = authService;
        _permissionService = permissionService;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            if (!_authService.IsAuthenticated())
            {
                return new AuthenticationState(Anonymous);
            }

            var adminEmail = _authService.GetCurrentAdminEmail();
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                return new AuthenticationState(Anonymous);
            }

            var roles = await _permissionService.GetRolesForAdminAsync(adminEmail);
            var permissions = await _permissionService.GetPermissionsForAdminAsync(adminEmail);
            var adminUserId = await _permissionService.GetCurrentAdminUserIdAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, adminEmail),
                new Claim(ClaimTypes.Email, adminEmail),
                new Claim(ClaimTypes.Role, "Admin")
            };

            if (adminUserId.HasValue)
            {
                claims.Add(new Claim(AdminClaims.AdminUserId, adminUserId.Value.ToString()));
            }

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(permissions.Select(permission => new Claim(AdminClaims.Permission, permission)));

            var identity = new ClaimsIdentity(claims, "AdminSession");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve admin authentication state");
            return new AuthenticationState(Anonymous);
        }
    }

    public void NotifyAdminAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
