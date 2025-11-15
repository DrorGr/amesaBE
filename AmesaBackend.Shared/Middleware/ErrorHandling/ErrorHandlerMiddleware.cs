using AmesaBackend.Shared.Contracts;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Data.Common;
using System.Net;
using AmesaBackend.Shared.Enums;

namespace AmesaBackend.Shared.Middleware.ErrorHandling
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _provider;
        private readonly bool _restExceptionDetails;

        /// <summary>
        /// Error handling middleware constructor
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="provider"></param>
        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IConfiguration configuration, IServiceProvider provider)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _provider = provider;
            _restExceptionDetails = configuration.GetValue<bool>("RestExceptionDetails:showExceptionDetails", false);
        }

        /// <summary>
        /// Invoke middleware
        /// </summary>
        /// <param name="context"></param>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var sessionId = context.User?.GetClaimValue(AMESAClaimTypes.SessionId.ToString());
                context.Request.EnableBuffering();
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                _logger.LogError(error, "SessionId: {Id}, Request Body: {RequestBody}", sessionId, requestBody);

                context.Response.ContentType = "application/json";
                ApiError apiError = null!;

                var newResponse = new ApiResponse<object>
                {
                    Version = GetApiVersion(),
                    Message = error.Message,
                    Data = null!,
                    Code = context.Response.StatusCode,
                    IsError = true,
                    ResponseException = null
                };

                switch (error)
                {
                    case ApiException ex:

                        if (ex.IsModelValidationError)
                        {
                            newResponse.Message = "Bad Input";
                            apiError = new ApiError(ResponseMessageEnum.ValidationError.GetDescription(), ex.Errors)
                            {
                                ReferenceErrorCode = ex.ReferenceErrorCode,
                                ReferenceDocumentLink = ex.ReferenceDocumentLink,
                            };
                        }
                        else
                            apiError = new ApiError(ex.Message);
                        context.Response.StatusCode = ex.StatusCode;
                        break;
                    case CustomFaultException e:
                        using (var scope = _provider.CreateScope())
                        {
                            var localizer = scope.ServiceProvider.GetService<IStringLocalizer>();

                            string localizedErrorDescription = localizer?[$"{e.StatusCode}"] ?? e.Message;
                            apiError = new ApiError(e.StatusCode.ToString())
                            {
                                ExceptionMessage = e.Message,
                                Details = $"{(int)e.StatusCode}: {localizedErrorDescription}"
                            };
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                        break;
                    case UnauthorizedAccessException:

                        apiError = new ApiError(ResponseMessageEnum.UnAuthorized.GetDescription());
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                        break;
                    case KeyNotFoundException:
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        apiError = new ApiError(error.Message);
                        break;
                    case DbException e:
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        apiError = new ApiError("DbException error")
                        {
                            ExceptionMessage = e.Message
                        };

                        break;

                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        apiError = new ApiError(error.Message);
                        break;

                }
                //add flag for environment               
                if (!_restExceptionDetails)
                {
                    apiError = new ApiError("All details show in log information")
                    {
                        ExceptionMessage = null
                    };
                }
                
                newResponse.ResponseException = apiError;
                newResponse.Code = context.Response.StatusCode;
                var result = JsonConvert.SerializeObject(newResponse, JSONSettings());
                await context.Response.WriteAsync(result);
            }
        }
        
        private string GetApiVersion()
        {
            //Extract Environment Variable from docker compose file (or docker file)
            //BUILD_VERSION example: "4.0.0-92"
            string buildVersion = Environment.GetEnvironmentVariable("BUILD_VERSION") ?? string.Empty;
            if (string.IsNullOrEmpty(buildVersion))
            {
                buildVersion = Environment.GetEnvironmentVariable("BUILD_VERSION_ENV") ?? string.Empty;
            }
            //Manipulate BUILD_VERSION string to be without the project's last version compiled  
            if (buildVersion is not null)
            {
                string delimiter = "-";
                string buildVersionTruncated = string.Empty;
                int lastIndexAddress = buildVersion.LastIndexOf(delimiter);
                if (lastIndexAddress != -1 && lastIndexAddress + delimiter.Length < buildVersion.Length)
                {
                    buildVersionTruncated = buildVersion.Substring(0, lastIndexAddress);
                }
                //return value for example: BUILD_VERSION="4.0.0"
                return String.IsNullOrEmpty(buildVersionTruncated) ? buildVersion : buildVersionTruncated;
            }
            return _configuration.GetSection("Version:Key").Get<string>() ?? "1.0.0"; // READ FROM DOCKER COMPOSE FILE
        }

        private JsonSerializerSettings JSONSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };
        }
    }
}

