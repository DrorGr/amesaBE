using Microsoft.AspNetCore.Http;

namespace AmesaBackend.Auth.Middleware
{
    public class IpTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpTrackingMiddleware> _logger;

        public IpTrackingMiddleware(RequestDelegate next, ILogger<IpTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract IP from X-Forwarded-For header (for ALB/CloudFront)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            string? clientIp = null;

            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                clientIp = forwardedFor.Split(',')[0].Trim();
            }
            else
            {
                // Fallback to RemoteIpAddress
                clientIp = context.Connection.RemoteIpAddress?.ToString();
            }

            // Store in HttpContext.Items for use by other middleware/services
            context.Items["ClientIp"] = clientIp ?? "unknown";

            // Device fingerprint: Hash(User-Agent + IP prefix)
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            if (!string.IsNullOrEmpty(clientIp) && !string.IsNullOrEmpty(userAgent))
            {
                var ipPrefix = clientIp.Split('.').Take(3).Aggregate((a, b) => $"{a}.{b}");
                var deviceFingerprint = $"{userAgent}|{ipPrefix}";
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(deviceFingerprint));
                var deviceId = Convert.ToBase64String(hash).Substring(0, 16);
                context.Items["DeviceId"] = deviceId;
            }

            await _next(context);
        }
    }
}

