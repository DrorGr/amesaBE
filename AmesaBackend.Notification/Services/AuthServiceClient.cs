using AmesaBackend.Shared.Rest;
using AmesaBackend.Notification.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;

namespace AmesaBackend.Notification.Services
{
    public interface IAuthServiceClient
    {
        Task<UserInfo?> GetUserInfoAsync(Guid userId);
        Task<List<Guid>> GetActiveUserIdsAsync();
        Task<List<Guid>> GetUserIdsBySegmentAsync(string segment); // "all", "active", "premium", etc.
    }

    public class AuthServiceClient : IAuthServiceClient
    {
        private readonly IHttpRequest _httpRequest;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthServiceClient> _logger;
        private readonly string _baseUrl;
        private readonly string? _serviceAuthToken;

        public AuthServiceClient(
            IHttpRequest httpRequest,
            IConfiguration configuration,
            ILogger<AuthServiceClient> logger)
        {
            _httpRequest = httpRequest;
            _configuration = configuration;
            _logger = logger;
            _baseUrl = configuration["Services:AuthService:Url"] 
                ?? "http://amesa-auth-service:8080";
            _serviceAuthToken = Environment.GetEnvironmentVariable("SERVICE_AUTH_API_KEY");
        }

        /// <summary>
        /// Gets user information by user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>User information, or null if error occurs</returns>
        public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var url = $"{_baseUrl}/api/v1/auth/users/{userId}";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<UserInfo>>(url, _serviceAuthToken ?? "", headers);
                return response?.Success == true ? response.Data : null;
            },
            operationName: $"GetUserInfoAsync({userId})",
            defaultValue: (UserInfo?)null);
        }

        /// <summary>
        /// Gets all active user IDs
        /// Note: This endpoint may not be fully implemented in the auth service
        /// </summary>
        /// <returns>List of active user IDs, or empty list if error occurs or endpoint not available</returns>
        public async Task<List<Guid>> GetActiveUserIdsAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                // This would require an endpoint in Auth service
                // For now, return empty - implement when endpoint is available
                var url = $"{_baseUrl}/api/v1/auth/users/active";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<List<UserIdDto>>>(url, _serviceAuthToken ?? "", headers);
                
                if (response?.Success == true && response.Data != null)
                {
                    return response.Data.Select(u => u.UserId).ToList();
                }
                
                _logger.LogWarning("GetActiveUserIdsAsync endpoint not available or returned empty");
                return new List<Guid>();
            },
            operationName: "GetActiveUserIdsAsync",
            defaultValue: new List<Guid>());
        }

        /// <summary>
        /// Gets user IDs by segment (e.g., "all", "active", "premium")
        /// Note: This endpoint may not be fully implemented in the auth service
        /// </summary>
        /// <param name="segment">The user segment to filter by</param>
        /// <returns>List of user IDs in the segment, or empty list if error occurs or endpoint not available</returns>
        public async Task<List<Guid>> GetUserIdsBySegmentAsync(string segment)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                // This would require an endpoint in Auth service for user segmentation
                var url = $"{_baseUrl}/api/v1/auth/users/segment/{segment}";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<List<UserIdDto>>>(url, _serviceAuthToken ?? "", headers);
                
                if (response?.Success == true && response.Data != null)
                {
                    return response.Data.Select(u => u.UserId).ToList();
                }
                
                _logger.LogWarning("GetUserIdsBySegmentAsync endpoint not available for segment {Segment}", segment);
                return new List<Guid>();
            },
            operationName: $"GetUserIdsBySegmentAsync({segment})",
            defaultValue: new List<Guid>());
        }

        private List<KeyValuePair<string, string>> GetAuthHeaders()
        {
            var headers = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(_serviceAuthToken))
            {
                headers.Add(new KeyValuePair<string, string>("X-Service-Auth", _serviceAuthToken));
            }
            return headers;
        }

        /// <summary>
        /// Executes an operation with retry logic for transient errors
        /// </summary>
        private async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            T defaultValue,
            int maxRetries = 3,
            int baseDelayMs = 1000)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    // Transient HTTP errors - retry with exponential backoff
                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning(ex, 
                        "Transient error in {OperationName} (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms", 
                        operationName, attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs);
                }
                catch (TaskCanceledException ex) when (attempt < maxRetries)
                {
                    // Timeout errors - retry with exponential backoff
                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning(ex, 
                        "Timeout in {OperationName} (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms", 
                        operationName, attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    // Non-transient errors or final attempt - log and return default
                    _logger.LogError(ex, "Error in {OperationName} (attempt {Attempt}/{MaxRetries})", 
                        operationName, attempt, maxRetries);
                    if (attempt == maxRetries)
                    {
                        return defaultValue;
                    }
                    // For other exceptions, retry once more
                    var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs);
                }
            }
            
            return defaultValue;
        }
    }

    public class UserInfo
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }
    }

    public class UserIdDto
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
    }

}
