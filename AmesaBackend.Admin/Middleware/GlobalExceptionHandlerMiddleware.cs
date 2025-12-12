using System.Net;
using System.Text.Json;
using Serilog;

namespace AmesaBackend.Admin.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var message = "An error occurred while processing your request.";
            var errorCode = "INTERNAL_ERROR";

            // Categorize exceptions
            if (exception is UnauthorizedAccessException)
            {
                code = HttpStatusCode.Unauthorized;
                message = "You don't have permission to perform this action.";
                errorCode = "UNAUTHORIZED";
            }
            else if (exception is ArgumentException || exception is ArgumentNullException)
            {
                code = HttpStatusCode.BadRequest;
                message = "Invalid request parameters.";
                errorCode = "VALIDATION_ERROR";
            }
            else
            {
                switch (exception)
                {
                case KeyNotFoundException:
                    code = HttpStatusCode.NotFound;
                    message = "The requested resource was not found.";
                    errorCode = "NOT_FOUND";
                    break;
                case InvalidOperationException:
                    code = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    errorCode = "BUSINESS_RULE_ERROR";
                    break;
                case TimeoutException:
                    code = HttpStatusCode.RequestTimeout;
                    message = "The request timed out. Please try again.";
                    errorCode = "TIMEOUT";
                    break;
                }
            }

            // Log the exception
            Log.Error(exception, "Unhandled exception: {ErrorCode} - {Message}", errorCode, exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode = (int)code;

            var result = JsonSerializer.Serialize(new
            {
                success = false,
                message = message,
                error = new
                {
                    code = errorCode,
                    details = exception.Message,
                    suggestion = GetErrorSuggestion(exception)
                }
            });

            return response.WriteAsync(result);
        }

        private static string GetErrorSuggestion(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => "Please ensure you are logged in and have the necessary permissions.",
                ArgumentException => "Please check your input and try again.",
                KeyNotFoundException => "The resource may have been deleted or moved.",
                TimeoutException => "The server may be experiencing high load. Please try again in a moment.",
                _ => "If this problem persists, please contact support."
            };
        }
    }

    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}

