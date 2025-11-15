using System.Net;

namespace AmesaBackend.Shared.Rest
{
    public interface IHttpRequest
    {
        /// <summary>
        /// Http get request which returns raw HttpResponseMessage
        /// </summary>
        Task<HttpResponseMessage?> GetRequest(string url, string token = "", string clientName = "");

        Task<T?> GetRequest<T>(string url, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "");

        Task<T?> PostRequest<T>(string url, object content, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "");

        Task<T?> PostRequest<T>(string url, string content, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "");

        Task<T?> DeleteRequest<T>(string url, object? content = null, string token = "", string clientName = "");

        Task<T?> PatchRequest<T>(string url, object content, string token = "", string clientName = "");

        Task<T?> PutRequest<T>(string url, object content, string token = "", string clientName = "");

        Task<HttpResult<T>> Post<T>(string url, object content, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "");

        Task<HttpResult<T>> Post<T>(string url, string content, string token = "",
            List<KeyValuePair<string, string>>? headers = null, string clientName = "");
    }
}

