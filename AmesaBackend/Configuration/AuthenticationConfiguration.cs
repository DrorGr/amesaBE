using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AmesaBackend.Services;
using AmesaBackend.Models;
using Serilog;

namespace AmesaBackend.Configuration;

public static class AuthenticationConfiguration
{
    /// <summary>
    /// Configures Google OAuth authentication with event handlers for user creation and token caching.
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
                options.SignInScheme = "Cookies"; // Use cookies for OAuth sign-in
                options.SaveTokens = true;

                // Log configuration for debugging
                Console.WriteLine($"[OAuth] Google ClientId: {googleClientId?.Substring(0, Math.Min(30, googleClientId?.Length ?? 0))}...");
                Console.WriteLine($"[OAuth] Google ClientSecret: {(string.IsNullOrWhiteSpace(googleClientSecret) ? "MISSING" : googleClientSecret.Substring(0, Math.Min(10, googleClientSecret.Length)) + "...")}");
                Console.WriteLine($"[OAuth] Google CallbackPath: {options.CallbackPath}");
                Console.WriteLine($"[OAuth] Frontend URL: {frontendUrl}");

                // Configure OAuth cookie for state management
                options.CorrelationCookie.Name = ".Amesa.Google.Correlation";
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.Path = "/";
                options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10);
                
                // For development (HTTP), use Lax. For production (HTTPS), use None with Secure
                // Note: Don't set Domain explicitly - let it default to the request domain
                // This ensures cookies work correctly with load balancers and proxies
                if (environment.IsDevelopment())
                {
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                }
                else
                {
                    // Production: Must use None for cross-site cookies, and Always for Secure
                    options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                }
                
                // Handle user creation when OAuth ticket is being created
                // This event fires when the OAuth response is received and ticket is being constructed
                // We create the user, generate JWT tokens, and set the redirect URI to frontend with temp token
                options.Events.OnCreatingTicket = async context =>
                {
                    try
                    {
                        // Get services from HttpContext
                        var serviceProvider = context.HttpContext.RequestServices;
                        var authService = serviceProvider.GetRequiredService<IAuthService>();
                        var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        
                        // #region agent log
                        var redirectUriBefore = context.Properties.RedirectUri ?? "NULL";
                        logger.LogInformation("[DEBUG] OnCreatingTicket:entry hypothesisId=A,B redirectUriBefore={RedirectUriBefore}", redirectUriBefore);
                        // #endregion
                        
                        // Get user info from claims (these are populated from Google's OAuth response)
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

                        // Create/update user and get JWT tokens
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

                        // Generate a temporary one-time token for token exchange with frontend
                        var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        
                        // Store JWT tokens in memory cache with 5 minute expiration, keyed by temp token
                        var cacheKey = $"oauth_token_{tempToken}";
                        memoryCache.Set(cacheKey, new OAuthTokenCache
                        {
                            AccessToken = authResponse.AccessToken,
                            RefreshToken = authResponse.RefreshToken,
                            ExpiresAt = authResponse.ExpiresAt,
                            IsNewUser = isNewUser,
                            UserAlreadyExists = !isNewUser
                        }, TimeSpan.FromMinutes(5));

                        // Store temp token keyed by email so we can retrieve it after redirect
                        // Also store in properties so callback endpoint can access it
                        var emailCacheKey = $"oauth_temp_token_{email}";
                        memoryCache.Set(emailCacheKey, tempToken, TimeSpan.FromMinutes(5));
                        context.Properties.Items["temp_token"] = tempToken;
                        
                        // Modify RedirectUri to include the code parameter
                        // This ensures the frontend receives the code for token exchange
                        var frontendUrlForRedirect = configuration["FrontendUrl"] ?? "http://localhost:4200";
                        var baseRedirectUri = context.Properties.RedirectUri ?? $"{frontendUrlForRedirect}/auth/callback";
                        
                        // #region agent log
                        var tempTokenPreview = tempToken?.Substring(0, Math.Min(10, tempToken?.Length ?? 0)) + "...";
                        logger.LogInformation("[DEBUG] OnCreatingTicket:before-modify hypothesisId=A,B,C baseRedirectUri={BaseRedirectUri} tempTokenPreview={TempTokenPreview}", baseRedirectUri, tempTokenPreview);
                        // #endregion
                        
                        // Append code parameter to redirect URI
                        if (!string.IsNullOrEmpty(tempToken))
                        {
                            var separator = baseRedirectUri.Contains("?") ? "&" : "?";
                            var modifiedRedirectUri = $"{baseRedirectUri}{separator}code={Uri.EscapeDataString(tempToken)}";
                            context.Properties.RedirectUri = modifiedRedirectUri;
                        
                            // #region agent log
                            logger.LogInformation("[DEBUG] OnCreatingTicket:after-modify hypothesisId=A,B,C redirectUriAfter={RedirectUriAfter} hasCode={HasCode}", modifiedRedirectUri, modifiedRedirectUri.Contains("code="));
                            // #endregion
                        }
                        
                        logger.LogInformation("OnCreatingTicket: Modified RedirectUri to include code parameter: {RedirectUri}", context.Properties.RedirectUri);
                        
                        logger.LogInformation("User created/updated and tokens cached for: {Email}, temp_token: {TempToken}, RedirectUri: {RedirectUri}", 
                            email, tempToken, context.Properties.RedirectUri);
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "Error in OnCreatingTicket for Google OAuth");
                        context.Fail("Error processing authentication");
                    }
                };
                
                options.Events.OnRemoteFailure = context =>
                {
                    var errorMessage = context.Failure?.Message ?? "Unknown error";
                    var innerException = context.Failure?.InnerException?.Message ?? "";
                    Log.Error("Google OAuth remote failure: {Error}, Inner: {Inner}", errorMessage, innerException);
                    Console.WriteLine($"[OAuth Error] {errorMessage}");
                    Console.WriteLine($"[OAuth Error Inner] {innerException}");
                    
                    // Check if this is a state validation error
                    if (errorMessage.Contains("state", StringComparison.OrdinalIgnoreCase) || 
                        innerException.Contains("state", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning("OAuth state validation failed - this may be due to cookie configuration issues");
                        Console.WriteLine("[OAuth] State validation failed - check correlation cookie settings");
                    }
                    
                    // Redirect to frontend with error
                    var frontendUrlForError = configuration["FrontendUrl"] ?? "http://localhost:4200";
                    context.Response.Redirect($"{frontendUrlForError}/auth/callback?error={Uri.EscapeDataString(errorMessage)}");
                    context.HandleResponse();
                    
                    return Task.CompletedTask;
                };
                
                options.Events.OnAccessDenied = context =>
                {
                    Log.Warning("Google OAuth access denied");
                    Console.WriteLine("[OAuth] Access denied");
                    
                    // Redirect to frontend with error
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

                // Configure OAuth cookie for state management
                options.CorrelationCookie.Name = ".Amesa.Meta.Correlation";
                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
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
    public static IServiceCollection AddMainAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
        });

        return services;
    }
}

