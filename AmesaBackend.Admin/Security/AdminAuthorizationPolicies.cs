using Microsoft.AspNetCore.Authorization;

namespace AmesaBackend.Admin.Security;

public static class AdminAuthorizationPolicies
{
    public static void AddPermissionPolicies(AuthorizationOptions options)
    {
        foreach (var permission in AdminPermissionNames.All)
        {
            options.AddPolicy(permission, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(AdminClaims.Permission, permission);
            });
        }
    }
}
