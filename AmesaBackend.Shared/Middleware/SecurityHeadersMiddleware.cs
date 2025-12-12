using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Shared.Middleware
{
    /// <summary>
    /// Middleware to add security headers to all HTTP responses.
    /// Implements OWASP security header recommendations.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers to response
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            
            // Content Security Policy (CSP) - adjust based on your needs
            // Note: SignalR WebSocket support (wss: ws:) is included for services that use SignalR
            // Added CDN sources for Bootstrap, Font Awesome, and SignalR
            // connect-src includes CDN for source map fetching
            var csp = "default-src 'self'; " +
                      "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com https://cdn.jsdelivr.net; " +
                      "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                      "img-src 'self' data: https:; " +
                      "font-src 'self' data: https://cdnjs.cloudflare.com; " +
                      "connect-src 'self' https://www.google.com https://cdn.jsdelivr.net wss: ws:; " +
                      "frame-ancestors 'none';";
            context.Response.Headers.Append("Content-Security-Policy", csp);
            
            // Strict Transport Security (HSTS) - only add in production with HTTPS
            if (context.Request.IsHttps)
            {
                context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }

            await _next(context);
        }
    }
}

