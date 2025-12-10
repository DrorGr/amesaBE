using AmesaBackend.Auth.Services;
using System.Security.Claims;

namespace AmesaBackend.Auth.Middleware
{
    /// <summary>
    /// Middleware to update session activity on each authenticated request.
    /// </summary>
    public class SessionActivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionActivityMiddleware> _logger;

        public SessionActivityMiddleware(RequestDelegate next, ILogger<SessionActivityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
        {
            // Only update activity for authenticated requests
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    // Get session token from JWT claims (added during token generation)
                    var sessionToken = context.User.FindFirst("session_token")?.Value;

                    if (!string.IsNullOrEmpty(sessionToken))
                    {
                        // Update session activity asynchronously (fire and forget for performance)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await sessionService.UpdateSessionActivityAsync(sessionToken);
                            }
                            catch (Exception ex)
                            {
                                // Log but don't fail the request
                                _logger.LogError(ex, "Error updating session activity");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    _logger.LogError(ex, "Error in session activity middleware");
                }
            }

            await _next(context);
        }
    }
}

