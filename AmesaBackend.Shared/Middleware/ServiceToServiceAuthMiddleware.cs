using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AmesaBackend.Shared.Middleware
{
    public class ServiceToServiceAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceToServiceAuthMiddleware> _logger;
        private readonly string _apiKey;

        public ServiceToServiceAuthMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<ServiceToServiceAuthMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["ServiceAuth:ApiKey"] 
                ?? Environment.GetEnvironmentVariable("SERVICE_AUTH_API_KEY") 
                ?? "";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only check for service-to-service endpoints
            var path = context.Request.Path.Value ?? "";
            if (path.Contains("/tickets/create-from-payment") || 
                path.Contains("/tickets/validate"))
            {
                var apiKey = context.Request.Headers["X-Service-Api-Key"].FirstOrDefault();

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Service-to-service API key not configured. Endpoints are unprotected.");
                    await _next(context);
                    return;
                }

                if (string.IsNullOrEmpty(apiKey) || apiKey != _apiKey)
                {
                    _logger.LogWarning("Invalid or missing service API key for {Path}", path);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized: Invalid service API key");
                    return;
                }
            }

            await _next(context);
        }
    }
}

