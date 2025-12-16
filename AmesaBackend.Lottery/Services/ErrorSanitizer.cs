using AmesaBackend.Lottery.DTOs;
using Microsoft.Extensions.Hosting;

namespace AmesaBackend.Lottery.Services
{
    public interface IErrorSanitizer
    {
        ErrorResponse Sanitize(Exception ex, bool isDevelopment);
    }

    public class ErrorSanitizer : IErrorSanitizer
    {
        private readonly IHostEnvironment _environment;

        public ErrorSanitizer(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public ErrorResponse Sanitize(Exception ex, bool isDevelopment)
        {
            // In development, show more details for debugging
            if (isDevelopment || _environment.IsDevelopment())
            {
                return new ErrorResponse
                {
                    Code = ex.GetType().Name,
                    Message = ex.Message,
                    Details = ex.StackTrace
                };
            }

            // In production, return generic messages only
            // Note: ArgumentOutOfRangeException must be checked before ArgumentException
            // because it inherits from ArgumentException
            return ex switch
            {
                KeyNotFoundException => new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = "The requested resource was not found"
                },
                UnauthorizedAccessException => new ErrorResponse
                {
                    Code = "UNAUTHORIZED",
                    Message = "You do not have permission to perform this action"
                },
                ArgumentOutOfRangeException => new ErrorResponse
                {
                    Code = "INVALID_INPUT",
                    Message = "The provided input is out of valid range"
                },
                ArgumentException => new ErrorResponse
                {
                    Code = "INVALID_INPUT",
                    Message = "The provided input is invalid"
                },
                InvalidOperationException => new ErrorResponse
                {
                    Code = "INVALID_OPERATION",
                    Message = "The requested operation cannot be performed"
                },
                TimeoutException => new ErrorResponse
                {
                    Code = "TIMEOUT",
                    Message = "The operation timed out. Please try again"
                },
                _ => new ErrorResponse
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while processing your request"
                }
            };
        }
    }
}



