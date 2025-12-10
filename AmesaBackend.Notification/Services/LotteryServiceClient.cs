using AmesaBackend.Shared.Rest;
using AmesaBackend.Notification.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;

namespace AmesaBackend.Notification.Services
{
    public interface ILotteryServiceClient
    {
        Task<List<Guid>> GetDrawParticipantsAsync(Guid drawId);
        Task<HouseInfo?> GetHouseInfoAsync(Guid houseId);
        Task<Guid?> GetHouseCreatorIdAsync(Guid houseId);
        Task<List<Guid>> GetHouseFavoriteUserIdsAsync(Guid houseId);
        Task<List<Guid>> GetHouseParticipantUserIdsAsync(Guid houseId);
    }

    public class LotteryServiceClient : ILotteryServiceClient
    {
        private readonly IHttpRequest _httpRequest;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LotteryServiceClient> _logger;
        private readonly string _baseUrl;
        private readonly string? _serviceAuthToken;

        public LotteryServiceClient(
            IHttpRequest httpRequest,
            IConfiguration configuration,
            ILogger<LotteryServiceClient> logger)
        {
            _httpRequest = httpRequest;
            _configuration = configuration;
            _logger = logger;
            _baseUrl = configuration["Services:LotteryService:Url"] 
                ?? "http://amesa-lottery-service:8080";
            _serviceAuthToken = Environment.GetEnvironmentVariable("SERVICE_AUTH_API_KEY");
        }

        /// <summary>
        /// Gets all participant user IDs for a specific lottery draw
        /// </summary>
        /// <param name="drawId">The draw ID</param>
        /// <returns>List of participant user IDs, or empty list if error occurs</returns>
        public async Task<List<Guid>> GetDrawParticipantsAsync(Guid drawId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var url = $"{_baseUrl}/api/v1/draws/{drawId}/participants";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<List<ParticipantDto>>>(url, _serviceAuthToken ?? "", headers);
                
                if (response?.Success == true && response.Data != null)
                {
                    return response.Data.Select(p => p.UserId).Distinct().ToList();
                }
                
                _logger.LogWarning("Failed to get participants for draw {DrawId}", drawId);
                return new List<Guid>();
            }, 
            operationName: $"GetDrawParticipantsAsync({drawId})",
            defaultValue: new List<Guid>());
        }

        /// <summary>
        /// Gets house information by house ID
        /// </summary>
        /// <param name="houseId">The house ID</param>
        /// <returns>House information, or null if error occurs</returns>
        public async Task<HouseInfo?> GetHouseInfoAsync(Guid houseId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var url = $"{_baseUrl}/api/v1/houses/{houseId}";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<HouseInfo>>(url, _serviceAuthToken ?? "", headers);
                
                return response?.Success == true ? response.Data : null;
            },
            operationName: $"GetHouseInfoAsync({houseId})",
            defaultValue: (HouseInfo?)null);
        }

        /// <summary>
        /// Gets the creator user ID for a specific house
        /// </summary>
        /// <param name="houseId">The house ID</param>
        /// <returns>Creator user ID, or null if error occurs</returns>
        public async Task<Guid?> GetHouseCreatorIdAsync(Guid houseId)
        {
            var houseInfo = await GetHouseInfoAsync(houseId);
            return houseInfo?.CreatedByUserId;
        }

        /// <summary>
        /// Gets all user IDs who have favorited a specific house
        /// </summary>
        /// <param name="houseId">The house ID</param>
        /// <returns>List of user IDs who favorited the house, or empty list if error occurs</returns>
        public async Task<List<Guid>> GetHouseFavoriteUserIdsAsync(Guid houseId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var url = $"{_baseUrl}/api/v1/houses/{houseId}/favorites";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<List<FavoriteDto>>>(url, _serviceAuthToken ?? "", headers);
                
                if (response?.Success == true && response.Data != null)
                {
                    return response.Data.Select(f => f.UserId).Distinct().ToList();
                }
                
                return new List<Guid>();
            },
            operationName: $"GetHouseFavoriteUserIdsAsync({houseId})",
            defaultValue: new List<Guid>());
        }

        /// <summary>
        /// Gets all participant user IDs for a specific house
        /// Note: This endpoint may not be fully implemented in the lottery service
        /// </summary>
        /// <param name="houseId">The house ID</param>
        /// <returns>List of participant user IDs, or empty list if error occurs or endpoint not available</returns>
        public async Task<List<Guid>> GetHouseParticipantUserIdsAsync(Guid houseId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                // Try to get participants from tickets/entries endpoint
                var url = $"{_baseUrl}/api/v1/houses/{houseId}/participants";
                var headers = GetAuthHeaders();
                
                var response = await _httpRequest.GetRequest<ApiResponse<ParticipantStatsDto>>(url, _serviceAuthToken ?? "", headers);
                
                // If this endpoint returns participant list, extract it
                // For now, return empty and log - endpoint may need to be implemented
                _logger.LogWarning("GetHouseParticipantUserIdsAsync needs implementation based on actual API for house {HouseId}", houseId);
                return new List<Guid>();
            },
            operationName: $"GetHouseParticipantUserIdsAsync({houseId})",
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

    // DTOs for lottery service responses
    public class ParticipantDto
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        
        [JsonPropertyName("ticketCount")]
        public int TicketCount { get; set; }
    }

    public class HouseInfo
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("createdByUserId")]
        public Guid CreatedByUserId { get; set; }
        
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class FavoriteDto
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        
        [JsonPropertyName("houseId")]
        public Guid HouseId { get; set; }
    }

    public class ParticipantStatsDto
    {
        [JsonPropertyName("totalParticipants")]
        public int TotalParticipants { get; set; }
        
        [JsonPropertyName("totalTickets")]
        public int TotalTickets { get; set; }
    }

}
