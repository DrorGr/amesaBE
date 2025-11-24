using System.Net;

namespace AmesaBackend.Shared.Rest
{
    public record HttpResult<T>(
        HttpStatusCode StatusCode,
        bool IsSuccess,
        string? Message,
        T? Value)
    {
        /// <summary>
        /// Creates a success result with a value.
        /// </summary>
        /// <param name="value">The result payload.</param>
        /// <param name="statusCode">The HTTP status code. Defaults to OK (200).</param>
        /// <returns>A new instance of <see cref="HttpResult{T}"/> representing a successful operation.</returns>
        public static HttpResult<T> Success(T? value, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            new(statusCode, true, string.Empty, value);

        /// <summary>
        /// Creates an empty result.
        /// </summary>
        /// <param name="statusCode">The HTTP status code. Defaults to NoContent (204).</param>
        /// <returns>A new instance of <see cref="HttpResult{T}"/> representing an empty operation.</returns>
        public static HttpResult<T> Empty(HttpStatusCode statusCode = HttpStatusCode.NoContent) =>
            new(statusCode, false, string.Empty, default);

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">The HTTP status code. Defaults to BadRequest (400).</param>
        /// <returns>A new instance of <see cref="HttpResult{T}"/> representing a failed operation.</returns>
        public static HttpResult<T> Failure(string? message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
            new(statusCode, false, message, default);
    }
}

