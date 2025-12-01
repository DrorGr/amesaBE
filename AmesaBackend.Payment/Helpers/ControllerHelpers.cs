using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Payment.DTOs;

namespace AmesaBackend.Payment.Helpers;

public static class ControllerHelpers
{
    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out userId))
        {
            return false;
        }
        
        return true;
    }

    public static ActionResult<ApiResponse<T>> UnauthorizedResponse<T>()
    {
        return new UnauthorizedObjectResult(new ApiResponse<T>
        {
            Success = false,
            Message = "Authentication required"
        });
    }

    public static string? GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault();
    }

    public static string? GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault();
    }
}

