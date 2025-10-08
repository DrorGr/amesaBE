using System.Net;
using System.Text.Json;

namespace AmesaBackend.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                error = new
                {
                    code = GetErrorCode(exception),
                    message = GetErrorMessage(exception),
                    details = GetErrorDetails(exception)
                },
                timestamp = DateTime.UtcNow
            };

            context.Response.StatusCode = GetStatusCode(exception);

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                NotImplementedException => (int)HttpStatusCode.NotImplemented,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        private static string GetErrorCode(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "UNAUTHORIZED",
                ArgumentException => "INVALID_ARGUMENT",
                InvalidOperationException => "INVALID_OPERATION",
                KeyNotFoundException => "NOT_FOUND",
                NotImplementedException => "NOT_IMPLEMENTED",
                _ => "INTERNAL_ERROR"
            };
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "Access denied",
                ArgumentException => "Invalid argument provided",
                InvalidOperationException => "Invalid operation",
                KeyNotFoundException => "Resource not found",
                NotImplementedException => "Feature not implemented",
                _ => "An internal error occurred"
            };
        }

        private static object? GetErrorDetails(Exception exception)
        {
            // In development, include more details
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
            return new
            {
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerMessage = exception.InnerException?.Message
            };
            }

            return null;
        }
    }
}
