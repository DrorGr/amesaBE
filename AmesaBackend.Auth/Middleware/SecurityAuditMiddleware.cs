using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;

namespace AmesaBackend.Auth.Middleware
{
    public class SecurityAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityAuditMiddleware> _logger;
        private static readonly HashSet<string> AuditPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/api/v1/auth/logout",
            "/api/v1/auth/refresh",
            "/api/v1/auth/verify-email",
            "/api/v1/auth/resend-verification",
            "/api/v1/auth/reset-password",
            "/api/v1/auth/forgot-password"
        };

        public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Only audit authentication-related endpoints
            if (!AuditPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var eventType = ExtractEventType(path, context.Request.Method);
            Guid? userId = null;

            // Try to get user ID from claims (may not be authenticated yet)
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // Capture request start time
            var startTime = DateTime.UtcNow;

            // Log after response (we'll need to check status code)
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Check final status code
            var success = context.Response.StatusCode < 400;
            var duration = DateTime.UtcNow - startTime;

            // Try to get user ID again after authentication (in case login succeeded)
            if (!userId.HasValue)
            {
                userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserIdAfterAuth))
                {
                    userId = parsedUserIdAfterAuth;
                }
            }

            // Log the event (non-blocking, but within request scope)
            // Note: For high-volume scenarios, consider using a background queue
            _ = Task.Run(async () =>
            {
                try
                {
                    // Create a new scope for the background task
                    using var scope = context.RequestServices.CreateScope();
                    var scopedAuditService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                    await scopedAuditService.LogAuthenticationEventAsync(
                        eventType,
                        userId,
                        success,
                        success ? null : $"HTTP {context.Response.StatusCode}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging audit event");
                }
            });

            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private string ExtractEventType(string path, string method)
        {
            if (path.Contains("/login", StringComparison.OrdinalIgnoreCase))
                return "Login";
            if (path.Contains("/register", StringComparison.OrdinalIgnoreCase))
                return "Registration";
            if (path.Contains("/logout", StringComparison.OrdinalIgnoreCase))
                return "Logout";
            if (path.Contains("/refresh", StringComparison.OrdinalIgnoreCase))
                return "TokenRefresh";
            if (path.Contains("/verify-email", StringComparison.OrdinalIgnoreCase))
                return "EmailVerification";
            if (path.Contains("/resend-verification", StringComparison.OrdinalIgnoreCase))
                return "ResendVerification";
            if (path.Contains("/reset-password", StringComparison.OrdinalIgnoreCase))
                return "PasswordReset";
            if (path.Contains("/forgot-password", StringComparison.OrdinalIgnoreCase))
                return "ForgotPassword";

            return "Unknown";
        }
    }
}

