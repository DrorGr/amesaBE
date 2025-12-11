using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Models;
using Amazon.EventBridge;
using Serilog;

namespace AmesaBackend.Auth.Configuration;

public static class AuthenticationConfiguration
{
    /// <summary>
    /// Configures Google OAuth authentication with event handlers for user creation, token caching, and error handling.
    /// </summary>
    public static AuthenticationBuilder AddGoogleOAuth(this AuthenticationBuilder authBuilder, IConfiguration configuration, IHostEnvironment environment)
    {
        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        var frontendUrl = configuration["FrontendUrl"] ?? 
                         configuration["AllowedOrigins:0"] ?? 
                         "https://dpqbvdgnenckf.cloudfront.net";

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/api/v1/oauth/google-callback";
                options.SignInScheme = "Cookies";
                options.SaveTokens = true;

                options.CorrelationCookie.Name = ".Amesa.Google.Correlation";
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.Path = "/";
                options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10);
                
                if (environment.IsDevelopment())
                {
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                }
                else
                {
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                }

                options.Events.OnCreatingTicket = async context =>
                {
                    try
                    {
                        var serviceProvider = context.HttpContext.RequestServices;
                        var authService = serviceProvider.GetRequiredService<IAuthService>();
                        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        
                        // #region agent log
                        var redirectUriBefore = context.Properties.RedirectUri ?? "NULL";
                        var callbackPath = context.Options.CallbackPath.ToString();
                        logger.LogInformation("[DEBUG] OnCreatingTicket:entry hypothesisId=A,B redirectUriBefore={RedirectUriBefore} callbackPath={CallbackPath}", redirectUriBefore, callbackPath);
                        // #endregion
                        
                        var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                        var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                        var googleId = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var firstName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value;
                        var lastName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname)?.Value;
                        
                        // Extract additional Google claims
                        var birthdateClaim = claims.FirstOrDefault(c => c.Type == "birthdate")?.Value;
                        var genderClaim = claims.FirstOrDefault(c => c.Type == "gender")?.Value;
                        var pictureClaim = claims.FirstOrDefault(c => c.Type == "picture")?.Value;
                        
                        DateTime? dateOfBirth = null;
                        if (!string.IsNullOrEmpty(birthdateClaim) && DateTime.TryParse(birthdateClaim, out var parsedDate))
                        {
                            dateOfBirth = parsedDate;
                        }
                        
                        string? gender = null;
                        if (!string.IsNullOrEmpty(genderClaim))
                        {
                            // Map Google gender to our enum (male/female/other)
                            gender = genderClaim.ToLower() switch
                            {
                                "male" => "Male",
                                "female" => "Female",
                                _ => "Other"
                            };
                        }

                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                        {
                            logger.LogWarning("Google OAuth ticket creation: missing email or ID");
                            context.Fail("Missing email or Google ID");
                            return;
                        }

                        logger.LogInformation("Google OAuth ticket being created for: {Email}", email);

                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:before-CreateOrUpdateOAuthUserAsync hypothesisId=E email={Email}", email);
                        // #endregion

                        var (authResponse, isNewUser) = await authService.CreateOrUpdateOAuthUserAsync(
                            email: email,
                            providerId: googleId,
                            provider: AuthProvider.Google,
                            firstName: firstName,
                            lastName: lastName,
                            dateOfBirth: dateOfBirth,
                            gender: gender,
                            profileImageUrl: pictureClaim
                        );

                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:after-CreateOrUpdateOAuthUserAsync hypothesisId=E hasAuthResponse={HasAuthResponse} isNewUser={IsNewUser}", authResponse != null, isNewUser);
                        logger.LogInformation("[DEBUG] OnCreatingTicket:checking-authResponse hypothesisId=E authResponseNull={AuthResponseNull} hasAccessToken={HasAccessToken} hasRefreshToken={HasRefreshToken}", 
                            authResponse == null, authResponse?.AccessToken != null, authResponse?.RefreshToken != null);
                        // #endregion

                        if (authResponse == null)
                        {
                            logger.LogError("[DEBUG] OnCreatingTicket:authResponse-is-null hypothesisId=E");
                            context.Fail("Failed to create authentication response");
                            return;
                        }

                        if (string.IsNullOrEmpty(authResponse.AccessToken))
                        {
                            logger.LogError("[DEBUG] OnCreatingTicket:accessToken-is-null-or-empty hypothesisId=E");
                            context.Fail("Failed to generate access token");
                            return;
                        }

                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:before-tempToken-generation hypothesisId=E");
                        // #endregion

                        var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        
                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:after-tempToken-generation hypothesisId=E tempTokenLength={TempTokenLength}", tempToken?.Length ?? 0);
                        // #endregion
                        
                        var cacheKey = $"oauth_token_{tempToken}";
                        
                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:before-cache-set hypothesisId=E cacheKey={CacheKey}", cacheKey);
                        // #endregion
                        
                        // Use distributed cache instead of memory cache for consistency across instances
                        var cacheExpiration = TimeSpan.FromMinutes(
                            configuration.GetValue<int>("SecuritySettings:OAuthTokenCacheExpirationMinutes", 5));
                        var cacheData = new OAuthTokenCache
                        {
                            AccessToken = authResponse.AccessToken,
                            RefreshToken = authResponse.RefreshToken,
                            ExpiresAt = authResponse.ExpiresAt,
                            IsNewUser = isNewUser,
                            UserAlreadyExists = !isNewUser
                        };
                        var cacheJson = System.Text.Json.JsonSerializer.Serialize(cacheData, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                        var cacheBytes = System.Text.Encoding.UTF8.GetBytes(cacheJson);
                        var cacheOptions = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = cacheExpiration
                        };
                        await distributedCache.SetAsync(cacheKey, cacheBytes, cacheOptions);

                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:after-cache-set hypothesisId=E");
                        // #endregion

                        // Use hashed email for cache key to protect privacy
                        var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())));
                        var emailCacheKey = $"oauth_temp_token_{emailHash}";
                        var emailCacheBytes = System.Text.Encoding.UTF8.GetBytes(tempToken);
                        await distributedCache.SetAsync(emailCacheKey, emailCacheBytes, cacheOptions);
                        context.Properties.Items["temp_token"] = tempToken;
                        
                        // #region agent log
                        var baseRedirectUri = context.Properties.RedirectUri ?? $"{frontendUrl}/auth/callback";
                        logger.LogInformation("[DEBUG] OnCreatingTicket:before-modify hypothesisId=A,B,C baseRedirectUri={BaseRedirectUri} tempTokenLength={TempTokenLength}", baseRedirectUri, tempToken?.Length ?? 0);
                        // Note: tempToken value is NOT logged for security
                        // #endregion
                        
                        var modifiedRedirectUri = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken ?? string.Empty)}";
                        context.Properties.RedirectUri = modifiedRedirectUri;
                        
                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:after-modify hypothesisId=A,B,C redirectUriAfter={RedirectUriAfter} hasCode={HasCode} contextPropertiesRedirectUri={ContextPropertiesRedirectUri}", 
                            modifiedRedirectUri, 
                            modifiedRedirectUri.Contains("code="),
                            context.Properties.RedirectUri);
                        // #endregion
                        
                        logger.LogInformation("User created/updated and tokens cached for: {Email}", email);
                        // Note: temp_token value is NOT logged for security
                        
                        // #region agent log
                        logger.LogInformation("[DEBUG] OnCreatingTicket:exit hypothesisId=A,B,C finalRedirectUri={FinalRedirectUri} tempTokenInProperties={TempTokenInProperties}", 
                            context.Properties.RedirectUri,
                            context.Properties.Items.ContainsKey("temp_token"));
                        // #endregion
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        
                        // #region agent log
                        var exType = ex.GetType().FullName;
                        var exMessage = ex.Message;
                        var isEventBridge = ex is Amazon.EventBridge.AmazonEventBridgeException;
                        var innerIsEventBridge = ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        logger.LogError(ex, "[DEBUG] OnCreatingTicket:catch exType={ExType} exMessage={ExMessage} isEventBridge={IsEventBridge} innerIsEventBridge={InnerIsEventBridge} hypothesisId=E", exType, exMessage, isEventBridge, innerIsEventBridge);
                        // #endregion
                        
                        // Check if this is an EventBridge exception - these are non-fatal and should not fail OAuth
                        var isEventBridgeException = ex is Amazon.EventBridge.AmazonEventBridgeException ||
                                                   ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        
                        if (isEventBridgeException)
                        {
                            logger.LogWarning(ex, "[DEBUG] OnCreatingTicket:EventBridge-detected (non-fatal, OAuth flow continues) hypothesisId=E");
                            logger.LogWarning(ex, "EventBridge error in OnCreatingTicket (non-fatal, OAuth flow continues)");
                            // Don't call context.Fail() for EventBridge errors - they're non-fatal
                            // The OAuth flow should continue even if EventBridge publishing fails
                            return;
                        }
                        
                        logger.LogError(ex, "[DEBUG] OnCreatingTicket:non-EventBridge-exception calling context.Fail() hypothesisId=E");
                        logger.LogError(ex, "Error in OnCreatingTicket for Google OAuth");
                        context.Fail("Error processing authentication");
                    }
                };
                
                options.Events.OnRemoteFailure = context =>
                {
                    var errorMessage = context.Failure?.Message ?? "Unknown error";
                    var errorDescription = context.Failure?.ToString() ?? "No additional details";
                    
                    // Extract security context information
                    var httpContext = context.HttpContext;
                    var clientIp = httpContext.Items["ClientIp"]?.ToString() 
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                        ?? "unknown";
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
                    var requestPath = httpContext.Request.Path.ToString();
                    var queryString = httpContext.Request.QueryString.ToString();
                    
                    // #region agent log
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                    logger?.LogError("[DEBUG] OnRemoteFailure:entry hypothesisId=A,B,C,D,E errorMessage={ErrorMessage}", errorMessage);
                    // #endregion
                    
                    // Enhanced logging with security context
                    Log.Error("Google OAuth remote failure: {Error}", errorMessage);
                    Log.Error("Google OAuth failure details: {Details}", errorDescription);
                    Log.Warning("OAuth Security Event - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}, Query: {QueryString}", 
                        clientIp, userAgent, requestPath, queryString);
                    
                    // Log the ClientId (first 10 chars for security) to verify it's being used
                    var clientId = configuration["Authentication:Google:ClientId"];
                    if (!string.IsNullOrWhiteSpace(clientId))
                    {
                        var clientIdPreview = clientId.Length > 10 ? clientId.Substring(0, 10) + "..." : clientId;
                        Log.Information("OAuth ClientId being used: {ClientIdPreview}", clientIdPreview);
                    }
                    else
                    {
                        Log.Warning("OAuth ClientId is null or empty!");
                    }
                    
                    // Check if it's an invalid_client error
                    if (errorMessage.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Error("INVALID_CLIENT ERROR: The ClientId or ClientSecret is incorrect or doesn't match Google Cloud Console");
                        Log.Error("Please verify:");
                        Log.Error("1. AWS Secrets Manager secret 'amesa-google_people_API' contains correct ClientId and ClientSecret");
                        Log.Error("2. The ClientId matches the OAuth 2.0 Client ID in Google Cloud Console");
                        Log.Error("3. The ClientSecret matches the OAuth 2.0 Client Secret in Google Cloud Console");
                    }
                    
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString(errorMessage)}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
                
                options.Events.OnAccessDenied = context =>
                {
                    // Extract security context information
                    var httpContext = context.HttpContext;
                    var clientIp = httpContext.Items["ClientIp"]?.ToString() 
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                        ?? "unknown";
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
                    var requestPath = httpContext.Request.Path.ToString();
                    
                    // #region agent log
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                    logger?.LogWarning("[DEBUG] OnAccessDenied:entry hypothesisId=A,B,C,D,E");
                    // #endregion
                    
                    Log.Warning("Google OAuth access denied - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}", 
                        clientIp, userAgent, requestPath);
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "http://localhost:4200";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString("Access denied")}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
                
                // Add OnTicketReceived to track when ticket is received (after OnCreatingTicket)
                options.Events.OnTicketReceived = context =>
                {
                    // #region agent log
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                    var redirectUri = context.Properties?.RedirectUri ?? "NULL";
                    var hasTempToken = context.Properties?.Items?.ContainsKey("temp_token") ?? false;
                    logger?.LogInformation("[DEBUG] OnTicketReceived:entry hypothesisId=A,B,C redirectUri={RedirectUri} hasTempToken={HasTempToken}", redirectUri, hasTempToken);
                    // #endregion
                    return Task.CompletedTask;
                };
            });
        }
        else
        {
            Log.Warning("Google OAuth credentials not configured. Google login will not work.");
        }

        return authBuilder;
    }

    /// <summary>
    /// Configures Meta/Facebook OAuth authentication.
    /// </summary>
    public static AuthenticationBuilder AddMetaOAuth(this AuthenticationBuilder authBuilder, IConfiguration configuration)
    {
        var metaAppId = configuration["Authentication:Meta:AppId"];
        var metaAppSecret = configuration["Authentication:Meta:AppSecret"];

        if (!string.IsNullOrWhiteSpace(metaAppId) && !string.IsNullOrWhiteSpace(metaAppSecret))
        {
            authBuilder.AddFacebook(options =>
            {
                options.AppId = metaAppId;
                options.AppSecret = metaAppSecret;
                options.CallbackPath = "/api/v1/oauth/meta-callback";
                options.SaveTokens = true;

                options.CorrelationCookie.Name = ".Amesa.Meta.Correlation";
                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;

                // Enhanced failure logging with security context
                options.Events.OnRemoteFailure = context =>
                {
                    var errorMessage = context.Failure?.Message ?? "Unknown error";
                    var errorDescription = context.Failure?.ToString() ?? "No additional details";
                    
                    // Extract security context information
                    var httpContext = context.HttpContext;
                    var clientIp = httpContext.Items["ClientIp"]?.ToString() 
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                        ?? "unknown";
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
                    var requestPath = httpContext.Request.Path.ToString();
                    var queryString = httpContext.Request.QueryString.ToString();
                    
                    Log.Error("Meta OAuth remote failure: {Error}", errorMessage);
                    Log.Error("Meta OAuth failure details: {Details}", errorDescription);
                    Log.Warning("OAuth Security Event (Meta) - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}, Query: {QueryString}", 
                        clientIp, userAgent, requestPath, queryString);
                    
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString(errorMessage)}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };

                options.Events.OnAccessDenied = context =>
                {
                    // Extract security context information
                    var httpContext = context.HttpContext;
                    var clientIp = httpContext.Items["ClientIp"]?.ToString() 
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                        ?? "unknown";
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
                    var requestPath = httpContext.Request.Path.ToString();
                    
                    Log.Warning("Meta OAuth access denied - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}", 
                        clientIp, userAgent, requestPath);
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString("Access denied")}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }
        else
        {
            Log.Warning("Meta OAuth credentials not configured. Meta login will not work.");
        }

        return authBuilder;
    }

    /// <summary>
    /// Configures authorization policies (AdminOnly, UserOrAdmin).
    /// </summary>
    public static IServiceCollection AddAuthAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
        });

        return services;
    }
}

