namespace AmesaBackend.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/api/oauth/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
            }
            else
            {
                context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            }

            // Add HSTS header for HTTPS
            if (context.Request.IsHttps)
            {
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            await _next(context);
        }
    }
}
