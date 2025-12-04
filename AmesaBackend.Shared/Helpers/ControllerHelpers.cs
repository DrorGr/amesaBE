using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace AmesaBackend.Shared.Helpers
{
    /// <summary>
    /// Shared controller helper methods for common operations
    /// </summary>
    public static class ControllerHelpers
    {
        /// <summary>
        /// Safely extracts and parses the user ID from claims. Returns false if claim is missing or invalid.
        /// </summary>
        /// <param name="user">The claims principal</param>
        /// <param name="userId">The parsed user ID, or Guid.Empty if parsing fails</param>
        /// <returns>True if user ID was successfully parsed, false otherwise</returns>
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

        /// <summary>
        /// Gets the user ID from claims, throwing an UnauthorizedAccessException if not found or invalid.
        /// </summary>
        /// <param name="user">The claims principal</param>
        /// <returns>The user ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if user ID claim is missing or invalid</exception>
        public static Guid GetUserId(ClaimsPrincipal user)
        {
            if (!TryGetUserId(user, out var userId))
            {
                throw new UnauthorizedAccessException("User ID claim is missing or invalid");
            }
            return userId;
        }

        /// <summary>
        /// Gets the IP address from the HTTP context, checking X-Forwarded-For and X-Real-IP headers.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>The IP address, or null if not available</returns>
        public static string? GetIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ??
                   context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                   context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        /// <summary>
        /// Gets the user agent from the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>The user agent, or null if not available</returns>
        public static string? GetUserAgent(HttpContext context)
        {
            return context.Request.Headers["User-Agent"].FirstOrDefault();
        }
    }
}

