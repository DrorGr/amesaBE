using Microsoft.AspNetCore.Http;
using AmesaBackend.Shared.Enums;

namespace AmesaBackend.Shared.Extensions
{
    public static class HttpContextAccessorExtensions
    {
        public static string? GetHeaderValue(this IHttpContextAccessor httpAccessor, string name)
        {
            return string.IsNullOrEmpty(name)
                ? null
                : httpAccessor.HttpContext?.Request.Headers.FirstOrDefault(x => x.Key == name).Value;
        }

        /// <summary>
        /// Gets language from token claim.
        /// </summary>
        /// <param name="httpAccessor"></param>
        /// <returns>Returns language id from token claim or 1 (default english)</returns>
        public static int GetUserLanguageId(this IHttpContextAccessor httpAccessor)
        {
            try
            {
                return httpAccessor.HttpContext?.User.GetLanguageId() ?? 1;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// Gets authorization token from the request header.
        /// </summary>
        /// <param name="httpAccessor"></param>
        /// <returns>Returns token or empty string if authorization token is not present.</returns>
        public static string GetToken(this IHttpContextAccessor httpAccessor)
        {
            var token = GetHeaderValue(httpAccessor, AMESAHeader.Authorization.GetValue());
            return token ?? string.Empty;
        }

        public static string GetUserId(this IHttpContextAccessor httpAccessor)
        {
            try
            {
                var userId = httpAccessor.HttpContext?.User.GetUserId();
                return userId?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetSessionId(this IHttpContextAccessor httpAccessor)
        {
            var sessionId = GetHeaderValue(httpAccessor, AMESAHeader.SessionId.GetValue());
            return sessionId ?? string.Empty;
        }

        public static string GetRequestAddress(this HttpContext context)
        {
            var address = context.Request.GetRequestAddress();
            if (!string.IsNullOrEmpty(address)) return address;

            address = context.Connection.RemoteIpAddress?.ToString();
            return !string.IsNullOrEmpty(address) ? address : "unknown";
        }

        public static string? GetRequestAddress(this HttpRequest request)
        {
            var address = request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(address)) return address;

            var addresses = request.Headers["X-Forwarded-For"].FirstOrDefault();
            address = (!string.IsNullOrEmpty(addresses)) ? addresses.Split(",").First().Trim() : null;

            return (!string.IsNullOrEmpty(address)) ? address : null;
        }
    }
}

