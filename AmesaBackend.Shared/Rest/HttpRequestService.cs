using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Text;

namespace AmesaBackend.Shared.Rest
{
    public class HttpRequestService : IHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpRequestService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly JsonSerializerSettings _jsonSettings;

        public HttpRequestService(
            IHttpClientFactory httpClientFactory,
            ILogger<HttpRequestService> logger,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public async Task<T?> PostRequest<T>(string url, object content, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "")
        {
            _logger.LogInformation("[PostRequest] {Url} - start  - content as object", url);
            var contentToString = JsonConvert.SerializeObject(content, _jsonSettings);
            return await PostRequest<T>(url, contentToString, token, headers, clientName);
        }

        public async Task<T?> PostRequest<T>(string url, string content, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "")
        {
            _logger.LogInformation("[PostRequest] {Url} - start  - content as string", url);
            using (var httpContent = new StringContent(content, Encoding.UTF8, "application/json"))
            {
                var httpClient = _httpClientFactory.CreateClient(clientName);
                try
                {
                    TryAddHttpClientHeaders(httpClient, token, headers);
                    int? timeoutSeconds = _configuration.GetValue<int?>("HttpClients:TimeoutSec");

                    httpClient.Timeout = timeoutSeconds.HasValue
                        ? TimeSpan.FromSeconds(timeoutSeconds.Value)
                        : TimeSpan.FromSeconds(100);

                    using (var response = await httpClient.PostAsync(url, httpContent))
                    {
                        var isSuccess = HandleHttpResponseAndLogs(response, url, "PostRequest");
                        if (!isSuccess)
                        {
                            return default;
                        }

                        var res = await response.Content.ReadAsStringAsync();
                        var serializedResponse = JsonConvert.DeserializeObject<T>(res, _jsonSettings);

                        return serializedResponse!;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "[PostRequest] {Url}, Error message: {Exception}", url, e.Message);
                    return default;
                }
            }
        }

        public async Task<HttpResult<T>> Post<T>(string url, object content, string token,
            List<KeyValuePair<string, string>>? headers = null, string clientName = "")
        {
            _logger.LogInformation("[Post] {Url} - start  - content as object", url);
            var contentToString = JsonConvert.SerializeObject(content, _jsonSettings);
            return await Post<T>(url, contentToString, token, headers, clientName);
        }

        public async Task<HttpResult<T>> Post<T>(string url, string content, string token,
            List<KeyValuePair<string, string>>? headers = null, string clientName = "")
        {
            _logger.LogInformation("[Post] {Url} - start  - content as string", url);

            using (var httpContent = new StringContent(content, Encoding.UTF8, "application/json"))
            {
                var httpClient = _httpClientFactory.CreateClient(clientName);
                try
                {
                    TryAddHttpClientHeaders(httpClient, token, headers);
                    int? timeoutSeconds = _configuration.GetValue<int?>("HttpClients:TimeoutSec");

                    httpClient.Timeout = timeoutSeconds.HasValue
                        ? TimeSpan.FromSeconds(timeoutSeconds.Value)
                        : TimeSpan.FromSeconds(100);
                    using (var response = await httpClient.PostAsync(url, httpContent))
                    {
                        HttpResult<T> result = await HandleHttpResponse<T>(response, url, "PostRequest");
                        return result;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "[Post] {Url}, Error message: {Exception}", url, e.Message);
                    return HttpResult<T>.Failure(e.Message);
                }
            }
        }

        private void TryAddHttpClientHeaders(System.Net.Http.HttpClient httpClient, string sentToken,
            List<KeyValuePair<string, string>>? headers = null)
        {
            try
            {
                string token = (sentToken == string.Empty ? GetTokenFromHttpContextAccessor() : sentToken);
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }

            try
            {
                var sessionId = _httpContextAccessor.GetSessionId();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("SessionId", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }

            if (headers != null && headers.Count > 0)
            {
                try
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        string headerName = header.Key;
                        string headerValue = header.Value;
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(headerName, headerValue);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Message}", ex.Message);
                }
            }
        }

        public async Task<T?> DeleteRequest<T>(string url, object? content = null, string token = "", string clientName = "")
        {
            _logger.LogInformation("[DeleteRequest] {Url} - start", url);
            var httpClient = _httpClientFactory.CreateClient(clientName);

            using (var httpContent = new StringContent(JsonConvert.SerializeObject(content, _jsonSettings),
                       Encoding.UTF8, "application/json"))
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, url);

                if (content != null)
                {
                    request.Content = httpContent;
                }

                try
                {
                    TryAddHttpClientHeaders(httpClient, token);

                    using (var response = await httpClient.SendAsync(request))
                    {
                        var isSuccess = HandleHttpResponseAndLogs(response, url, "DeleteRequest");
                        if (!isSuccess)
                        {
                            return default;
                        }

                        var serializedResponse =
                            JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), _jsonSettings);

                        return serializedResponse!;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "[DeleteRequest] {Url}, Error message: {Exception}", url, ex.Message);
                    return default;
                }
            }
        }

        public async Task<T?> PatchRequest<T>(string url, object content, string token = "", string clientName = "")
        {
            _logger.LogInformation("[PatchRequest] {Url} - start", url);

            var httpClient = _httpClientFactory.CreateClient(clientName);
            using (var httpContent = new StringContent(JsonConvert.SerializeObject(content, _jsonSettings),
                       Encoding.UTF8, "application/json"))
            {
                try
                {
                    TryAddHttpClientHeaders(httpClient, token);

                    using (var response = await httpClient.PatchAsync(url, httpContent))
                    {
                        var isSuccess = HandleHttpResponseAndLogs(response, url, "PatchRequest");
                        if (!isSuccess)
                        {
                            return default;
                        }

                        var serializedResponse =
                            JsonConvert.DeserializeObject<T?>(await response.Content.ReadAsStringAsync(), _jsonSettings);
                        return serializedResponse;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "[PatchRequest] {Url}, Error message: {Exception}", url, e.Message);
                    return default;
                }
            }
        }

        public async Task<T?> PutRequest<T>(string url, object content, string token = "", string clientName = "")
        {
            _logger.LogInformation("[PutRequest] {Url} - start", url);
            var httpClient = _httpClientFactory.CreateClient(clientName);
            using (var httpContent = new StringContent(JsonConvert.SerializeObject(content, _jsonSettings),
                       Encoding.UTF8, "application/json"))
            {
                try
                {
                    TryAddHttpClientHeaders(httpClient, token);

                    using (var response = await httpClient.PutAsync(url, httpContent))
                    {
                        var isSuccess = HandleHttpResponseAndLogs(response, url, "PutRequest");
                        if (!isSuccess)
                        {
                            return default;
                        }

                        var serializedResponse =
                            JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), _jsonSettings);

                        return serializedResponse!;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "[PutRequest] {Url}, Error message: {Exception}", url, e.Message);
                    return default;
                }
            }
        }

        public async Task<T?> GetRequest<T>(string url, string token = "", List<KeyValuePair<string, string>>? headers = null, string clientName = "")
        {
            _logger.LogInformation("[GetRequest] {Url} - start", url);
            var httpClient = _httpClientFactory.CreateClient(clientName);

            try
            {
                TryAddHttpClientHeaders(httpClient, token, headers);
                using (var response = await httpClient.GetAsync(url))
                {
                    var isSuccess = HandleHttpResponseAndLogs(response, url, "GetRequest");
                    if (!isSuccess)
                    {
                        return default;
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("[GetRequest] {Url} - finished. Content: {ResponseString}", url, responseString);
                    var serializedResponse = JsonConvert.DeserializeObject<T>(responseString, _jsonSettings);
                    return serializedResponse;
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "[GetRequest] {Url}, Error message: {Exception}", url, e.Message);
                return default;
            }
        }

        public async Task<HttpResponseMessage?> GetRequest(string url, string token = "", string clientName = "")
        {
            _logger.LogInformation("[GetRequest] {Url} - start", url);
            var httpClient = _httpClientFactory.CreateClient(clientName);
            try
            {
                TryAddHttpClientHeaders(httpClient, token);

                var response = await httpClient.GetAsync(url);
                var isSuccess = HandleHttpResponseAndLogs(response, url, "GetRequest");

                return response;
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "[GetRequest] {Url}, Error message: {Exception}", url, e.Message);
                return default(HttpResponseMessage);
            }
        }

        private string GetTokenFromHttpContextAccessor()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var token = _httpContextAccessor.GetHeaderValue(AMESAHeader.Authorization.GetValue());
                return token ?? string.Empty;
            }

            return string.Empty;
        }

        private bool HandleHttpResponseAndLogs(HttpResponseMessage response, string url, string method)
        {
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("[{Method}] {Url} - finished, StatusCode: {StatusCode}", method, url,
                    response.StatusCode.ToString());
                return true;
            }

            if (response != null && response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("[{Method}] {Url} - finished, StatusCode: {StatusCode}", method, url,
                    response.StatusCode.ToString());
                return false;
            }

            _logger.LogInformation("[{Method}] {Url} - finished, StatusCode: {StatusCode}", method,
                url, response?.StatusCode.ToString());

            return false;
        }

        private async Task<HttpResult<T>> HandleHttpResponse<T>(HttpResponseMessage? response, string url, string method)
        {
            if (response == null)
            {
                _logger.LogInformation("[{Method}] {Url} - finished, No Response", method, url);
                return HttpResult<T>.Failure("No Response");
            }

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[{Method}] {Url} - finished, StatusCode: {StatusCode}", method, url,
                    response.StatusCode);

                var valueStr = await response.Content.ReadAsStringAsync();
                var value = JsonConvert.DeserializeObject<T>(valueStr, _jsonSettings);

                return HttpResult<T>.Success(value, response.StatusCode);
            }

            var payload = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("[{Method}] {Url} - finished, StatusCode: {StatusCode} and Payload : {Payload}", method,
                url, response.StatusCode, payload);
            return HttpResult<T>.Failure(payload, response.StatusCode);
        }
    }
}

