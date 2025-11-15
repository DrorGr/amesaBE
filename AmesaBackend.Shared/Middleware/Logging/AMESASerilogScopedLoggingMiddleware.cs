using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Shared.Middleware.Logging
{
    public class AMESASerilogScopedLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AMESASerilogScopedLoggingMiddleware> _logger;

        public static readonly List<string> RequestHeaders = new List<string>();
        public static readonly List<string> ResponseHeaders = new List<string>();

        public AMESASerilogScopedLoggingMiddleware(RequestDelegate next, ILogger<AMESASerilogScopedLoggingMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var loggerState = new Dictionary<string, object>
            {
                ["CorrelationID"] = Guid.NewGuid().ToString("N")
                //Add any number of properties to be logged under a single scope
            };

            var uniqueRequestHeaders = context.Request.Headers
                .Where(x => RequestHeaders.All(r => r != x.Key))
                .Select(x => x.Key);

            loggerState.Add("uniqueRequestHeaders", uniqueRequestHeaders);

            using (_logger.BeginScope<Dictionary<string, object>>(loggerState))
            {
                await _next(context);
            }

            var uniqueResponseHeaders = context.Response.Headers
                .Where(x => ResponseHeaders.All(r => r != x.Key))
                .Select(x => x.Key);
            loggerState.Add("uniqueResponseHeaders", uniqueResponseHeaders);
        }
    }
}

