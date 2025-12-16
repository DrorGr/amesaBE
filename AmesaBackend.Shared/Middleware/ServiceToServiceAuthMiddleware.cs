using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;
using System.Linq;

namespace AmesaBackend.Shared.Middleware
{
    public class ServiceToServiceAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceToServiceAuthMiddleware> _logger;
        private readonly string _apiKey;
        private readonly string[] _allowedHeaders;
        private readonly string[]? _ipWhitelist;

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
            
            // Support multiple header names for API key
            _allowedHeaders = new[] { "X-Service-Api-Key", "X-Service-Auth" };
            
            // Get IP whitelist from configuration
            var ipWhitelistConfig = _configuration.GetSection("ServiceAuth:IpWhitelist").Get<string[]>();
            _ipWhitelist = ipWhitelistConfig?.Length > 0 ? ipWhitelistConfig : null;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a service-to-service endpoint (marked with [AllowAnonymous] and specific paths)
            var path = context.Request.Path.Value ?? "";
            var isServiceEndpoint = path.Contains("/tickets/create-from-payment") || 
                                   path.Contains("/tickets/validate") ||
                                   path.Contains("/draws/") && path.Contains("/participants") ||
                                   path.Contains("/houses/") && (path.Contains("/favorites") || path.Contains("/participants/list")) ||
                                   path.Contains("/payments/refund");

            if (isServiceEndpoint)
            {
                // Get client IP address
                var clientIp = GetClientIpAddress(context);
                
                // Check IP whitelist if configured
                if (_ipWhitelist != null && _ipWhitelist.Length > 0)
                {
                    var isIpAllowed = _ipWhitelist.Any(ip => 
                        clientIp == ip || 
                        clientIp.StartsWith(ip + ".") || 
                        ip == "*"); // Allow wildcard
                    
                    if (!isIpAllowed)
                    {
                        _logger.LogWarning(
                            "Service-to-service request from unauthorized IP {ClientIp} to {Path}",
                            clientIp, path);
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Forbidden: IP address not whitelisted");
                        return;
                    }
                }

                // Check API key from any allowed header
                var apiKey = _allowedHeaders
                    .Select(header => context.Request.Headers[header].FirstOrDefault())
                    .FirstOrDefault(key => !string.IsNullOrEmpty(key));

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Service-to-service API key not configured. Endpoints are unprotected.");
                    // Log the request for audit
                    _logger.LogInformation(
                        "Service-to-service request to {Path} from IP {ClientIp} - API key not configured",
                        path, clientIp);
                    await _next(context);
                    return;
                }

                if (string.IsNullOrEmpty(apiKey) || apiKey != _apiKey)
                {
                    _logger.LogWarning(
                        "Invalid or missing service API key for {Path} from IP {ClientIp}",
                        path, clientIp);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized: Invalid service API key");
                    return;
                }

                // Audit log successful service-to-service authentication
                _logger.LogInformation(
                    "Service-to-service request authenticated: {Path} from IP {ClientIp}",
                    path, clientIp);
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check X-Forwarded-For header (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }

            // Check X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}

