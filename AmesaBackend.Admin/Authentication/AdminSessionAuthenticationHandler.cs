using System.Security.Claims;
using System.Text.Encodings.Web;
using AmesaBackend.Admin.Security;
using AmesaBackend.Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AmesaBackend.Admin.Authentication;

public sealed class AdminSessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AdminSession";

    public AdminSessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            if (Context.Session == null || !Context.Session.IsAvailable)
            {
                return AuthenticateResult.NoResult();
            }

            var adminEmail = Context.Session.GetString("AdminEmail");
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                return AuthenticateResult.NoResult();
            }

            var permissionService = Context.RequestServices.GetService<IAdminPermissionService>();
            var roles = permissionService == null
                ? new[] { "Admin" }
                : await permissionService.GetRolesForAdminAsync(adminEmail);
            var permissions = permissionService == null
                ? AdminPermissionNames.All
                : await permissionService.GetPermissionsForAdminAsync(adminEmail);
            var adminUserId = permissionService == null
                ? null
                : await permissionService.GetCurrentAdminUserIdAsync();

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

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to authenticate admin session");
            return AuthenticateResult.NoResult();
        }
    }
}
