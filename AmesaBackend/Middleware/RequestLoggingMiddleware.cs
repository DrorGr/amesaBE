using System.Diagnostics;

namespace AmesaBackend.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // Add request ID to context for tracing
            context.Items["RequestId"] = requestId;

            // Log request
            _logger.LogInformation(
                "Request {RequestId} started: {Method} {Path} from {RemoteIp}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            try
            {
                if (context.Request.Path.HasValue &&
                    context.Request.Path.Value.Contains("google-callback", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "OAuth callback cookies: {Cookies}",
                        context.Request.Headers["Cookie"].ToString());
                }

                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                _logger.LogInformation(
                    "Request {RequestId} completed: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
