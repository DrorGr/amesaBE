using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Services;
using AmesaBackend.Models;
using AmesaBackend.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;

namespace AmesaBackend.Controllers
{
    [ApiController]
    [Route("api/v1/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IWebHostEnvironment _environment;

        public OAuthController(
            IAuthService authService,
            IConfiguration configuration,
            ILogger<OAuthController> logger,
            IMemoryCache memoryCache,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
            _environment = environment;
        }

        /// <summary>
        /// Initiate Google OAuth login
        /// </summary>
        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? 
                                 _configuration.GetSection("AllowedOrigins").Get<string[]>()?[0] ?? 
                                 "https://dpqbvdgnenckf.cloudfront.net";

                // Check if Google OAuth is configured
                var googleClientId = _configuration["Authentication:Google:ClientId"];
                var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
                
                if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
                {
                    _logger.LogWarning("Google OAuth not configured - missing ClientId or ClientSecret");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_NOT_CONFIGURED",
                            Message = "Google OAuth is not configured. Please configure ClientId and ClientSecret in appsettings.json or AWS Secrets Manager.",
                            Details = new Dictionary<string, object>
                            {
                                { "provider", "Google" },
                                { "missing", string.IsNullOrWhiteSpace(googleClientId) ? "ClientId" : "ClientSecret" }
                            }
                        }
                    });
                }

                _logger.LogInformation("Initiating Google OAuth login");
                
                // Challenge should return a ChallengeResult that triggers a 302 redirect
                // The OAuth middleware will:
                // 1. Set correlation cookie and redirect to Google
                // 2. Google redirects back to CallbackPath (/api/v1/oauth/google-callback)
                // 3. OAuth middleware validates state (checks correlation cookie)
                // 4. OnCreatingTicket fires and creates user, stores temp_token, modifies RedirectUri to include code
                // 5. OAuth middleware redirects to RedirectUri (frontend with code)
                // Set RedirectUri to frontend - OnCreatingTicket will modify it to include the code parameter
                var initialRedirectUri = $"{frontendUrl}/auth/callback";
                
                // #region agent log
                _logger.LogInformation("[DEBUG] GoogleLogin hypothesisId=A,E initialRedirectUri={InitialRedirectUri} frontendUrl={FrontendUrl}", initialRedirectUri, frontendUrl);
                // #endregion
                
                var properties = new AuthenticationProperties
                {
                    RedirectUri = initialRedirectUri, // Frontend callback - OnCreatingTicket will add code parameter
                    AllowRefresh = true
                };
                
                return Challenge(properties, GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google OAuth");
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Failed to initiate Google login")}");
            }
        }

        /// <summary>
        /// Handle Google OAuth callback
        /// NOTE: This endpoint may not be reached because OAuth middleware intercepts the callback.
        /// User creation now happens in OnCreatingTicket event in Program.cs.
        /// This endpoint serves as a fallback if middleware redirects here.
        /// </summary>
        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                // #region agent log
                var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
                _logger.LogInformation("[DEBUG] GoogleCallback:entry hypothesisId=D requestUrl={RequestUrl} queryString={QueryString} hasCodeInQuery={HasCodeInQuery}", 
                    requestUrl, Request.QueryString.ToString(), Request.Query.ContainsKey("code"));
                // #endregion
                
                _logger.LogInformation("Google OAuth callback endpoint hit");
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";

                // Try to authenticate with Google scheme (in case middleware didn't handle it)
                var googleResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                _logger.LogInformation("Google OAuth callback: Authentication result - Succeeded: {Succeeded}, Principal: {HasPrincipal}", 
                    googleResult.Succeeded, googleResult.Principal != null);
                
                if (!googleResult.Succeeded)
                {
                    _logger.LogWarning("Google OAuth callback: Google authentication failed or not authenticated");
                    // If not authenticated via middleware, this is a fallback path
                    // The middleware should have handled redirect already
                    return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Google authentication failed")}");
                }

                // If we reach here, middleware handled it and redirected here
                // Get email from authenticated principal (used in multiple places)
                var email = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                _logger.LogInformation("Google OAuth callback: Email from principal: {Email}", email ?? "NULL");
                
                // Try to get temp_token from authentication properties (set in OnCreatingTicket)
                string? tempToken = null;
                bool hasTempTokenInProperties = false;
                if (googleResult.Properties?.Items.TryGetValue("temp_token", out var token) == true && token != null)
                {
                    tempToken = token;
                    hasTempTokenInProperties = true;
                }
                if (string.IsNullOrEmpty(tempToken))
                {
                    tempToken = Request.Query["temp_token"].FirstOrDefault();
                }
                
                _logger.LogInformation("Google OAuth callback: temp_token from properties: {HasToken}, Value: {TokenPreview}", 
                    hasTempTokenInProperties, tempToken?.Length > 0 ? tempToken.Substring(0, Math.Min(10, tempToken.Length)) + "..." : "NULL");
                
                // If not found in properties, try to get it from email cache (fallback)
                if (string.IsNullOrEmpty(tempToken) && !string.IsNullOrEmpty(email))
                {
                    var emailCacheKey = $"oauth_temp_token_{email}";
                    if (_memoryCache.TryGetValue(emailCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
                    {
                        tempToken = cachedToken;
                        _logger.LogInformation("Google OAuth callback: Found temp_token from email cache");
                        // Remove from cache after use
                        _memoryCache.Remove(emailCacheKey);
                    }
                    else
                    {
                        _logger.LogWarning("Google OAuth callback: temp_token not found in email cache for email: {Email}", email);
                    }
                }
                
                if (!string.IsNullOrEmpty(tempToken))
                {
                    _logger.LogInformation("Google OAuth callback: Found temp_token, redirecting to frontend with code");
                    await HttpContext.SignOutAsync("Cookies");
                    await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                    var redirectUrl = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}";
                    
                    // #region agent log
                    _logger.LogInformation("[DEBUG] GoogleCallback:redirect hypothesisId=D redirectUrl={RedirectUrl} hasCode={HasCode}", redirectUrl, redirectUrl.Contains("code="));
                    // #endregion
                    
                    _logger.LogInformation("Google OAuth callback: Redirecting to: {RedirectUrl}", redirectUrl);
                    return Redirect(redirectUrl);
                }
                
                _logger.LogWarning("Google OAuth callback: temp_token is empty after all attempts");

                // Fallback: try to get user from authenticated principal
                if (!string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Fallback: Processing Google OAuth callback for: {Email}", email);
                    // This should not normally happen as OnCreatingTicket should handle it
                    // But if it does, create user here as fallback
                    var googleId = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var firstName = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
                    var lastName = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;

                    if (!string.IsNullOrEmpty(googleId))
                    {
                        var authResponse = await _authService.CreateOrUpdateOAuthUserAsync(
                            email: email,
                            providerId: googleId,
                            provider: AuthProvider.Google,
                            firstName: firstName,
                            lastName: lastName
                        );

                        var fallbackTempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        var cacheKey = $"oauth_token_{fallbackTempToken}";
                        _memoryCache.Set(cacheKey, new OAuthTokenCache
                        {
                            AccessToken = authResponse.Response.AccessToken,
                            RefreshToken = authResponse.Response.RefreshToken,
                            ExpiresAt = authResponse.Response.ExpiresAt
                        }, TimeSpan.FromMinutes(5));

                        await HttpContext.SignOutAsync("Cookies");
                        await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                        return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(fallbackTempToken)}");
                    }
                }

                _logger.LogWarning("Google OAuth callback: Could not process callback - redirecting to frontend with error");
                await HttpContext.SignOutAsync("Cookies");
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Error processing authentication")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google OAuth callback");
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
                return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Error processing authentication")}");
            }
        }

        /// <summary>
        /// Initiate Meta/Facebook OAuth login
        /// </summary>
        [HttpGet("meta")]
        public IActionResult MetaLogin()
        {
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? 
                                 _configuration.GetSection("AllowedOrigins").Get<string[]>()?[0] ?? 
                                 "https://dpqbvdgnenckf.cloudfront.net";

                // Check if Meta OAuth is configured
                var metaAppId = _configuration["Authentication:Meta:AppId"];
                var metaAppSecret = _configuration["Authentication:Meta:AppSecret"];
                
                if (string.IsNullOrWhiteSpace(metaAppId) || string.IsNullOrWhiteSpace(metaAppSecret))
                {
                    _logger.LogWarning("Meta OAuth not configured - missing AppId or AppSecret");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_NOT_CONFIGURED",
                            Message = "Meta OAuth is not configured. Please configure AppId and AppSecret in appsettings.json or AWS Secrets Manager.",
                            Details = new Dictionary<string, object>
                            {
                                { "provider", "Meta" },
                                { "missing", string.IsNullOrWhiteSpace(metaAppId) ? "AppId" : "AppSecret" }
                            }
                        }
                    });
                }

                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(MetaCallback)),
                    Items =
                    {
                        { "scheme", "Facebook" },
                        { "returnUrl", frontendUrl }
                    }
                };

                _logger.LogInformation("Initiating Meta OAuth login");
                return Challenge(properties, FacebookDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Meta OAuth");
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Failed to initiate Meta login")}");
            }
        }

        /// <summary>
        /// Handle Meta/Facebook OAuth callback
        /// </summary>
        [HttpGet("meta-callback")]
        public async Task<IActionResult> MetaCallback()
        {
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? 
                                 _configuration.GetSection("AllowedOrigins").Get<string[]>()?[0] ?? 
                                 "https://dpqbvdgnenckf.cloudfront.net";

                var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Meta authentication failed");
                    return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Meta authentication failed")}");
                }

                var claims = result.Principal?.Claims.ToList() ?? new List<Claim>();
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var metaId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
                var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(metaId))
                {
                    _logger.LogWarning("Meta authentication: missing email or ID");
                    return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Invalid Meta authentication data")}");
                }

                _logger.LogInformation("Meta authentication successful for email: {Email}", email);

                // Create or update user
                var authResponse = await _authService.CreateOrUpdateOAuthUserAsync(
                    email: email,
                    providerId: metaId,
                    provider: AuthProvider.Meta,
                    firstName: firstName,
                    lastName: lastName
                );

                // Generate a temporary one-time token for token exchange
                var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                
                // Store tokens in memory cache with 5 minute expiration
                var cacheKey = $"oauth_token_{tempToken}";
                _memoryCache.Set(cacheKey, new OAuthTokenCache
                {
                    AccessToken = authResponse.Response.AccessToken,
                    RefreshToken = authResponse.Response.RefreshToken,
                    ExpiresAt = authResponse.Response.ExpiresAt
                }, TimeSpan.FromMinutes(5));

                // Redirect to frontend with temporary token
                return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Meta OAuth callback");
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Error processing Meta authentication")}");
            }
        }

        /// <summary>
        /// Exchange temporary OAuth token for JWT tokens
        /// </summary>
        [HttpPost("exchange")]
        [AllowAnonymous]
        public IActionResult ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new { error = "Code is required" });
                }

                var cacheKey = $"oauth_token_{request.Code}";
                _logger.LogInformation("Attempting to exchange token with cache key: {CacheKey}", cacheKey);
                
                if (!_memoryCache.TryGetValue(cacheKey, out OAuthTokenCache? cachedData) || cachedData == null)
                {
                    _logger.LogWarning("Invalid or expired OAuth exchange token. Code length: {CodeLength}, Code preview: {CodePreview}", 
                        request.Code?.Length ?? 0, 
                        request.Code?.Length > 10 ? request.Code.Substring(0, 10) + "..." : request.Code);
                    return Unauthorized(new { error = "Invalid or expired token" });
                }
                
                _logger.LogInformation("Successfully retrieved tokens from cache for code");

                // Remove token from cache (one-time use)
                _memoryCache.Remove(cacheKey);

                return Ok(new
                {
                    accessToken = cachedData.AccessToken,
                    refreshToken = cachedData.RefreshToken,
                    expiresAt = cachedData.ExpiresAt,
                    isNewUser = cachedData.IsNewUser,
                    userAlreadyExists = cachedData.UserAlreadyExists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging OAuth token");
                return StatusCode(500, new { error = "Error exchanging token" });
            }
        }
    }

    public class ExchangeTokenRequest
    {
        public string Code { get; set; } = string.Empty;
    }

}

