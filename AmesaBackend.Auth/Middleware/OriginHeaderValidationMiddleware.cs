using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Auth.Middleware
{
    /// <summary>
    /// Middleware to validate Origin header for CSRF protection.
    /// Validates that the Origin header matches allowed origins for state-changing requests.
    /// </summary>
    public class OriginHeaderValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OriginHeaderValidationMiddleware> _logger;
        private readonly HashSet<string> _allowedOrigins;
        private readonly bool _requireOriginHeader;

        // State-changing HTTP methods that require Origin validation
        private static readonly HashSet<string> StateChangingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "PATCH", "DELETE"
        };

        public OriginHeaderValidationMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<OriginHeaderValidationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
            
            // Get allowed origins from configuration
            var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>() ?? 
                               new[] { "http://localhost:4200" };
            _allowedOrigins = new HashSet<string>(allowedOrigins, StringComparer.OrdinalIgnoreCase);
            
            // Require Origin header in production, optional in development
            _requireOriginHeader = _configuration.GetValue<bool>("SecuritySettings:RequireOriginHeader", true);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for GET, HEAD, OPTIONS requests (read-only)
            if (!StateChangingMethods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Skip validation for health check endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/hc"))
            {
                await _next(context);
                return;
            }

            // Get Origin header
            var origin = context.Request.Headers.Origin.ToString();
            var referer = context.Request.Headers.Referer.ToString();

            // For same-origin requests, Origin header may be missing (browser behavior)
            // Check if request is same-origin by comparing Host header
            var isSameOrigin = IsSameOriginRequest(context);

            if (isSameOrigin)
            {
                // Same-origin request - Origin header not required
                await _next(context);
                return;
            }

            // For cross-origin requests, Origin header is required
            if (string.IsNullOrWhiteSpace(origin))
            {
                // Try to extract origin from Referer header as fallback
                if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                {
                    origin = $"{refererUri.Scheme}://{refererUri.Authority}";
                }
            }

            // Validate Origin header
            if (string.IsNullOrWhiteSpace(origin))
            {
                if (_requireOriginHeader)
                {
                    _logger.LogWarning(
                        "CSRF protection: Missing Origin header for {Method} request to {Path} from {RemoteIp}",
                        context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);
                    
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        error = new
                        {
                            code = "CSRF_PROTECTION_FAILED",
                            message = "Origin header is required for this request"
                        }
                    });
                    return;
                }
                else
                {
                    // In development, log warning but allow request
                    _logger.LogWarning(
                        "CSRF protection: Missing Origin header for {Method} request to {Path} (allowed in development)",
                        context.Request.Method, context.Request.Path);
                    await _next(context);
                    return;
                }
            }

            // Validate Origin matches allowed origins
            if (!_allowedOrigins.Contains(origin))
            {
                _logger.LogWarning(
                    "CSRF protection: Invalid Origin header '{Origin}' for {Method} request to {Path} from {RemoteIp}. Allowed origins: {AllowedOrigins}",
                    origin, context.Request.Method, context.Request.Path, 
                    context.Connection.RemoteIpAddress, string.Join(", ", _allowedOrigins));
                
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    error = new
                    {
                        code = "CSRF_PROTECTION_FAILED",
                        message = $"Origin '{origin}' is not allowed"
                    }
                });
                return;
            }

            // Origin is valid - continue
            await _next(context);
        }

        /// <summary>
        /// Checks if the request is same-origin by comparing Host header with Origin/Referer.
        /// </summary>
        private bool IsSameOriginRequest(HttpContext context)
        {
            var host = context.Request.Host.Value;
            var origin = context.Request.Headers.Origin.ToString();
            var referer = context.Request.Headers.Referer.ToString();

            // If no Origin or Referer, assume same-origin (browser behavior)
            if (string.IsNullOrWhiteSpace(origin) && string.IsNullOrWhiteSpace(referer))
            {
                return true;
            }

            // Extract host from Origin
            if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            {
                return string.Equals(originUri.Authority, host, StringComparison.OrdinalIgnoreCase);
            }

            // Extract host from Referer
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return string.Equals(refererUri.Authority, host, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }

    /// <summary>
    /// Extension method to register OriginHeaderValidationMiddleware.
    /// </summary>
    public static class OriginHeaderValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseOriginHeaderValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OriginHeaderValidationMiddleware>();
        }
    }
}



