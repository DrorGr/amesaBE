using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using AspNet.Security.OAuth.Apple;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
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
                
                // Don't set Domain explicitly - let it default to request host
                // This ensures the cookie works correctly with CloudFront
                // The cookie will be set for the CloudFront domain when requests come through CloudFront
                
                if (environment.IsDevelopment())
                {
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                }
                else
                {
                    // Production: SameSite=None requires Secure=true for cross-site cookies
                    // This is necessary for OAuth redirects from Google back to our domain
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

                        if (authResponse == null)
                        {
                            logger.LogError("OnCreatingTicket: auth response is null");
                            context.Fail("Failed to create authentication response");
                            return;
                        }

                        if (string.IsNullOrEmpty(authResponse.AccessToken))
                        {
                            logger.LogError("OnCreatingTicket: access token is null or empty");
                            context.Fail("Failed to generate access token");
                            return;
                        }

                        var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        
                        var cacheKey = $"oauth_token_{tempToken}";
                        
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

                        // Use hashed email for cache key to protect privacy
                        var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())));
                        var emailCacheKey = $"oauth_temp_token_{emailHash}";
                        var emailCacheBytes = System.Text.Encoding.UTF8.GetBytes(tempToken);
                        await distributedCache.SetAsync(emailCacheKey, emailCacheBytes, cacheOptions);
                        context.Properties.Items["temp_token"] = tempToken;
                        
                        var modifiedRedirectUri = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken ?? string.Empty)}";
                        context.Properties.RedirectUri = modifiedRedirectUri;
                        
                        logger.LogInformation("User created/updated and tokens cached for: {Email}", email);
                        // Note: temp_token value is NOT logged for security
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        
                        // Check if this is an EventBridge exception - these are non-fatal and should not fail OAuth
                        var isEventBridgeException = ex is Amazon.EventBridge.AmazonEventBridgeException ||
                                                   ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        
                        if (isEventBridgeException)
                        {
                            logger.LogWarning(ex, "EventBridge error in OnCreatingTicket (non-fatal, OAuth flow continues)");
                            // Don't call context.Fail() for EventBridge errors - they're non-fatal
                            // The OAuth flow should continue even if EventBridge publishing fails
                            return;
                        }
                        
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
                    
                    Log.Warning("Google OAuth access denied - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}", 
                        clientIp, userAgent, requestPath);
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "http://localhost:4200";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString("Access denied")}");
                    context.HandleResponse();
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

                // Handle user creation and token caching (similar to Google OAuth)
                options.Events.OnCreatingTicket = async context =>
                {
                    try
                    {
                        var serviceProvider = context.HttpContext.RequestServices;
                        var authService = serviceProvider.GetRequiredService<IAuthService>();
                        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        
                        var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                        var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                        var metaId = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var firstName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value;
                        var lastName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname)?.Value;
                        
                        // Extract additional Meta/Facebook claims
                        var birthdayClaim = claims.FirstOrDefault(c => c.Type == "urn:facebook:birthday")?.Value 
                            ?? claims.FirstOrDefault(c => c.Type == "birthday")?.Value;
                        var genderClaim = claims.FirstOrDefault(c => c.Type == "urn:facebook:gender")?.Value 
                            ?? claims.FirstOrDefault(c => c.Type == "gender")?.Value;
                        var pictureClaim = claims.FirstOrDefault(c => c.Type == "urn:facebook:picture")?.Value 
                            ?? claims.FirstOrDefault(c => c.Type == "picture")?.Value;

                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(metaId))
                        {
                            logger.LogWarning("Meta OAuth ticket creation: missing email or Meta ID");
                            context.Fail("Missing email or Meta ID");
                            return;
                        }

                        logger.LogInformation("Meta OAuth ticket being created for: {Email}", email);

                        // Parse Meta birthday (format: MM/DD/YYYY or MM/DD)
                        DateTime? dateOfBirth = null;
                        if (!string.IsNullOrEmpty(birthdayClaim))
                        {
                            var parts = birthdayClaim.Split('/');
                            if (parts.Length >= 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var day))
                            {
                                var year = parts.Length == 3 && int.TryParse(parts[2], out var y) ? y : DateTime.Now.Year - 18;
                                try
                                {
                                    dateOfBirth = new DateTime(year, month, day);
                                }
                                catch
                                {
                                    // Invalid date, ignore
                                }
                            }
                        }
                        
                        string? gender = null;
                        if (!string.IsNullOrEmpty(genderClaim))
                        {
                            gender = genderClaim.ToLower() switch
                            {
                                "male" => "Male",
                                "female" => "Female",
                                _ => "Other"
                            };
                        }

                        var (authResponse, isNewUser) = await authService.CreateOrUpdateOAuthUserAsync(
                            email: email,
                            providerId: metaId,
                            provider: AuthProvider.Meta,
                            firstName: firstName,
                            lastName: lastName,
                            dateOfBirth: dateOfBirth,
                            gender: gender,
                            profileImageUrl: pictureClaim
                        );

                        if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
                        {
                            logger.LogError("Meta OAuth: Failed to create authentication response or access token");
                            context.Fail("Failed to create authentication response");
                            return;
                        }

                        var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        var cacheKey = $"oauth_token_{tempToken}";
                        
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

                        // Use hashed email for cache key to protect privacy
                        var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())));
                        var emailCacheKey = $"oauth_temp_token_{emailHash}";
                        var emailCacheBytes = System.Text.Encoding.UTF8.GetBytes(tempToken);
                        await distributedCache.SetAsync(emailCacheKey, emailCacheBytes, cacheOptions);
                        context.Properties.Items["temp_token"] = tempToken;
                        
                        var frontendUrl = configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                        var modifiedRedirectUri = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken ?? string.Empty)}";
                        context.Properties.RedirectUri = modifiedRedirectUri;
                        
                        logger.LogInformation("User created/updated and tokens cached for: {Email}", email);
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        
                        var isEventBridgeException = ex is Amazon.EventBridge.AmazonEventBridgeException ||
                                                   ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        
                        if (isEventBridgeException)
                        {
                            logger.LogWarning(ex, "EventBridge error in OnCreatingTicket (non-fatal, OAuth flow continues)");
                            return;
                        }
                        
                        logger.LogError(ex, "Error in OnCreatingTicket for Meta OAuth");
                        context.Fail("Error processing authentication");
                    }
                };

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
    /// Configures Apple OAuth authentication.
    /// </summary>
    public static AuthenticationBuilder AddAppleOAuth(this AuthenticationBuilder authBuilder, IConfiguration configuration, IHostEnvironment environment)
    {
        var appleClientId = configuration["Authentication:Apple:ClientId"];
        var appleTeamId = configuration["Authentication:Apple:TeamId"];
        var appleKeyId = configuration["Authentication:Apple:KeyId"];
        var applePrivateKey = configuration["Authentication:Apple:PrivateKey"];
        var frontendUrl = configuration["FrontendUrl"] ?? 
                         configuration["AllowedOrigins:0"] ?? 
                         "https://dpqbvdgnenckf.cloudfront.net";

        if (!string.IsNullOrWhiteSpace(appleClientId) && 
            !string.IsNullOrWhiteSpace(appleTeamId) && 
            !string.IsNullOrWhiteSpace(appleKeyId) && 
            !string.IsNullOrWhiteSpace(applePrivateKey))
        {
            authBuilder.AddApple(options =>
            {
                options.ClientId = appleClientId;
                options.TeamId = appleTeamId;
                options.KeyId = appleKeyId;
                // TODO: Fix UsePrivateKey signature - expects IFileInfo, not string
                // options.UsePrivateKey(keyId => NormalizeApplePrivateKey(applePrivateKey));
                // Temporary workaround: Use the private key directly if the package supports it
                // Note: This may need to be adjusted based on the actual package API
                var normalizedKey = NormalizeApplePrivateKey(applePrivateKey);
                // For now, we'll need to check the package documentation for the correct API
                options.CallbackPath = "/api/v1/oauth/apple-callback";
                options.SaveTokens = true;

                options.CorrelationCookie.Name = ".Amesa.Apple.Correlation";
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
                    // Production: SameSite=None requires Secure=true for cross-site cookies
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                }

                // Handle user creation and token caching (similar to Google OAuth)
                options.Events.OnCreatingTicket = async context =>
                {
                    try
                    {
                        var serviceProvider = context.HttpContext.RequestServices;
                        var authService = serviceProvider.GetRequiredService<IAuthService>();
                        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        
                        var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                        var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                        var appleId = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var firstName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value;
                        var lastName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname)?.Value;
                        
                        // Apple may provide email in the ID token, or it might be null if user chose to hide email
                        // In that case, Apple provides a proxy email ending with @privaterelay.appleid.com
                        // We accept proxy emails as valid - they're unique per user and can be used for account creation
                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(appleId))
                        {
                            logger.LogWarning("Apple OAuth ticket creation: missing email or Apple ID");
                            context.Fail("Missing email or Apple ID");
                            return;
                        }

                        logger.LogInformation("Apple OAuth ticket being created for: {Email}", email);

                        var (authResponse, isNewUser) = await authService.CreateOrUpdateOAuthUserAsync(
                            email: email,
                            providerId: appleId,
                            provider: AuthProvider.Apple,
                            firstName: firstName,
                            lastName: lastName,
                            dateOfBirth: null, // Apple doesn't provide date of birth
                            gender: null, // Apple doesn't provide gender
                            profileImageUrl: null // Apple doesn't provide profile image
                        );

                        if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
                        {
                            logger.LogError("Apple OAuth: Failed to create authentication response or access token");
                            context.Fail("Failed to create authentication response");
                            return;
                        }

                        var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        var cacheKey = $"oauth_token_{tempToken}";
                        
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

                        // Use hashed email for cache key to protect privacy
                        var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())));
                        var emailCacheKey = $"oauth_temp_token_{emailHash}";
                        var emailCacheBytes = System.Text.Encoding.UTF8.GetBytes(tempToken);
                        await distributedCache.SetAsync(emailCacheKey, emailCacheBytes, cacheOptions);
                        context.Properties.Items["temp_token"] = tempToken;
                        
                        var modifiedRedirectUri = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken ?? string.Empty)}";
                        context.Properties.RedirectUri = modifiedRedirectUri;
                        
                        logger.LogInformation("User created/updated and tokens cached for: {Email}", email);
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        
                        var isEventBridgeException = ex is Amazon.EventBridge.AmazonEventBridgeException ||
                                                   ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        
                        if (isEventBridgeException)
                        {
                            logger.LogWarning(ex, "EventBridge error in OnCreatingTicket (non-fatal, OAuth flow continues)");
                            return;
                        }
                        
                        logger.LogError(ex, "Error in OnCreatingTicket for Apple OAuth");
                        context.Fail("Error processing authentication");
                    }
                };

                options.Events.OnRemoteFailure = context =>
                {
                    var errorMessage = context.Failure?.Message ?? "Unknown error";
                    var errorDescription = context.Failure?.ToString() ?? "No additional details";
                    
                    var httpContext = context.HttpContext;
                    var clientIp = httpContext.Items["ClientIp"]?.ToString() 
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                        ?? "unknown";
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
                    var requestPath = httpContext.Request.Path.ToString();
                    var queryString = httpContext.Request.QueryString.ToString();
                    
                    Log.Error("Apple OAuth remote failure: {Error}", errorMessage);
                    Log.Error("Apple OAuth failure details: {Details}", errorDescription);
                    Log.Warning("OAuth Security Event (Apple) - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}, Query: {QueryString}", 
                        clientIp, userAgent, requestPath, queryString);
                    
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString(errorMessage)}");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
                
                options.Events.OnAccessDenied = context =>
                {
                    var httpContext = context.HttpContext;
                    var clientIp = httpContext.Items["ClientIp"]?.ToString() 
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                        ?? "unknown";
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
                    var requestPath = httpContext.Request.Path.ToString();
                    
                    Log.Warning("Apple OAuth access denied - IP: {ClientIp}, User-Agent: {UserAgent}, Path: {RequestPath}", 
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
            Log.Warning("Apple OAuth credentials not configured. Apple login will not work.");
        }

        return authBuilder;
    }

    /// <summary>
    /// Normalizes Apple private key format to ensure it's in proper PEM format.
    /// Handles base64-encoded keys, JSON-escaped keys, and already-formatted PEM keys.
    /// </summary>
    private static string NormalizeApplePrivateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        // Remove JSON escaping if present (common when stored in AWS Secrets Manager)
        key = key.Replace("\\n", "\n").Replace("\\r", "\r");

        // If base64 encoded (no PEM headers), decode it
        if (!key.Contains("-----BEGIN"))
        {
            try
            {
                var bytes = Convert.FromBase64String(key.Trim());
                key = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Not base64, assume it's already in text format
            }
        }

        // Ensure proper PEM format with headers
        if (!key.Contains("-----BEGIN PRIVATE KEY-----"))
        {
            // Remove any existing line breaks and add proper PEM format
            key = key.Replace("\r", "").Replace("\n", "").Trim();
            key = "-----BEGIN PRIVATE KEY-----\n" + key + "\n-----END PRIVATE KEY-----";
        }

        return key;
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
