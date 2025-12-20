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
                StandardErrorResponse? errorResponse = null;
                int statusCode = (int)HttpStatusCode.InternalServerError;
                string errorCode = "INTERNAL_ERROR";
                string errorMessage = "An error occurred while processing your request.";

                switch (error)
                {
                    case ApiException ex:
                        statusCode = ex.StatusCode;
                        if (ex.IsModelValidationError)
                        {
                            errorCode = "VALIDATION_ERROR";
                            errorMessage = ResponseMessageEnum.ValidationError.GetDescription();
                            errorResponse = new StandardErrorResponse
                            {
                                Code = errorCode,
                                Message = errorMessage,
                                Details = ex.Errors?.Select(e => new { Field = e.Field, Message = e.Message }) ?? Enumerable.Empty<object>()
                            };
                        }
                        else
                        {
                            errorCode = ex.ReferenceErrorCode ?? "API_ERROR";
                            errorMessage = ex.Message;
                            errorResponse = new StandardErrorResponse
                            {
                                Code = errorCode,
                                Message = errorMessage,
                                Details = _restExceptionDetails ? ex.StackTrace : null
                            };
                        }
                        break;
                    case CustomFaultException e:
                        using (var scope = _provider.CreateScope())
                        {
                            var localizer = scope.ServiceProvider.GetService<IStringLocalizer>();
                            string localizedErrorDescription = localizer?[$"{e.StatusCode}"] ?? e.Message;
                            statusCode = (int)HttpStatusCode.InternalServerError;
                            errorCode = e.StatusCode.ToString();
                            errorMessage = localizedErrorDescription;
                            errorResponse = new StandardErrorResponse
                            {
                                Code = errorCode,
                                Message = errorMessage,
                                Details = _restExceptionDetails ? e.Message : null
                            };
                        }
                        break;
                    case UnauthorizedAccessException:
                        statusCode = (int)HttpStatusCode.Forbidden;
                        errorCode = "AUTHENTICATION_ERROR";
                        errorMessage = ResponseMessageEnum.UnAuthorized.GetDescription();
                        errorResponse = new StandardErrorResponse
                        {
                            Code = errorCode,
                            Message = errorMessage
                        };
                        break;
                    case KeyNotFoundException:
                        statusCode = (int)HttpStatusCode.NotFound;
                        errorCode = "NOT_FOUND";
                        errorMessage = error.Message;
                        errorResponse = new StandardErrorResponse
                        {
                            Code = errorCode,
                            Message = errorMessage
                        };
                        break;
                    case DbException e:
                        // Check if it's a database connectivity issue (DbUpdateException from EF Core)
                        // DbUpdateException inherits from DbException, so we check by exception type name
                        var exceptionTypeName = e.GetType().FullName;
                        if (exceptionTypeName != null && exceptionTypeName.Contains("DbUpdateException"))
                        {
                            statusCode = (int)HttpStatusCode.ServiceUnavailable;
                            errorCode = "SERVICE_UNAVAILABLE";
                            errorMessage = "Service is temporarily unavailable. Please try again later.";
                        }
                        else
                        {
                            statusCode = (int)HttpStatusCode.InternalServerError;
                            errorCode = "DATABASE_ERROR";
                            errorMessage = "A database error occurred.";
                        }
                        errorResponse = new StandardErrorResponse
                        {
                            Code = errorCode,
                            Message = errorMessage,
                            Details = _restExceptionDetails ? e.Message : null
                        };
                        break;
                    default:
                        statusCode = (int)HttpStatusCode.InternalServerError;
                        errorCode = "INTERNAL_ERROR";
                        errorMessage = _restExceptionDetails ? error.Message : "An internal server error occurred.";
                        errorResponse = new StandardErrorResponse
                        {
                            Code = errorCode,
                            Message = errorMessage,
                            Details = _restExceptionDetails ? error.StackTrace : null
                        };
                        break;
                }

                var newResponse = new StandardApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Message = errorMessage,
                    Error = errorResponse,
                    Timestamp = DateTime.UtcNow
                };

                context.Response.StatusCode = statusCode;
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

