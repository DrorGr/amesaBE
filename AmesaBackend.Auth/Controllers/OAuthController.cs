using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace AmesaBackend.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthController> _logger;
        private readonly IMemoryCache _memoryCache;

        public OAuthController(
            IAuthService authService,
            IConfiguration configuration,
            ILogger<OAuthController> logger,
            IMemoryCache memoryCache)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
        }

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
                var properties = new AuthenticationProperties
                {
                    RedirectUri = $"{frontendUrl}/auth/callback", // Frontend callback - OnCreatingTicket will add code parameter
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

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";

                // #region agent log
                _logger.LogInformation("[DEBUG] GoogleCallback:entry hypothesisId=C,D queryString={QueryString} hasCodeParam={HasCodeParam} hasErrorParam={HasErrorParam}", 
                    Request.QueryString.ToString(), 
                    Request.Query.ContainsKey("code"), 
                    Request.Query.ContainsKey("error"));
                // #endregion

                var googleResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                // #region agent log
                _logger.LogInformation("[DEBUG] GoogleCallback:after-AuthenticateAsync hypothesisId=C,D succeeded={Succeeded} hasPrincipal={HasPrincipal} hasProperties={HasProperties}", 
                    googleResult.Succeeded, 
                    googleResult.Principal != null, 
                    googleResult.Properties != null);
                // #endregion
                
                if (!googleResult.Succeeded)
                {
                    _logger.LogWarning("Google OAuth callback: Google authentication failed or not authenticated");
                    return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Google authentication failed")}");
                }

                // Get email from authenticated principal (used in multiple places)
                var email = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                // #region agent log
                var propertiesItemsCount = googleResult.Properties?.Items?.Count ?? 0;
                var hasTempTokenInProperties = googleResult.Properties?.Items?.ContainsKey("temp_token") ?? false;
                var redirectUri = googleResult.Properties?.RedirectUri;
                _logger.LogInformation("[DEBUG] GoogleCallback:before-tempToken-lookup hypothesisId=C,D email={Email} propertiesItemsCount={PropertiesItemsCount} hasTempTokenInProperties={HasTempTokenInProperties} redirectUri={RedirectUri}", 
                    email, propertiesItemsCount, hasTempTokenInProperties, redirectUri);
                // #endregion
                
                // Try to get temp_token from authentication properties (set in OnCreatingTicket)
                var tempToken = googleResult.Properties?.Items.TryGetValue("temp_token", out var token) == true 
                    ? token 
                    : Request.Query["temp_token"].FirstOrDefault();
                
                // #region agent log
                _logger.LogInformation("[DEBUG] GoogleCallback:after-tempToken-lookup hypothesisId=C,D tempTokenFound={TempTokenFound} tempTokenLength={TempTokenLength} source={Source}", 
                    !string.IsNullOrEmpty(tempToken), 
                    tempToken?.Length ?? 0,
                    googleResult.Properties?.Items.TryGetValue("temp_token", out _) == true ? "Properties" : Request.Query.ContainsKey("temp_token") ? "Query" : "None");
                // #endregion
                
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
                }
                
                if (!string.IsNullOrEmpty(tempToken))
                {
                    _logger.LogInformation("Google OAuth callback: Found temp_token, redirecting to frontend with code");
                    await HttpContext.SignOutAsync("Cookies");
                    await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                    return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}");
                }
                
                _logger.LogWarning("Google OAuth callback: temp_token not found in properties or email cache, using fallback");
                if (!string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Fallback: Processing Google OAuth callback for: {Email}", email);
                    var googleId = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var firstName = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
                    var lastName = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;
                    
                    // Extract additional Google claims
                    var birthdateClaim = googleResult.Principal?.FindFirst("birthdate")?.Value;
                    var genderClaim = googleResult.Principal?.FindFirst("gender")?.Value;
                    var pictureClaim = googleResult.Principal?.FindFirst("picture")?.Value;
                    
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

                    if (!string.IsNullOrEmpty(googleId))
                    {
                        var authResponse = await _authService.CreateOrUpdateOAuthUserAsync(
                            email: email,
                            providerId: googleId,
                            provider: AuthProvider.Google,
                            firstName: firstName,
                            lastName: lastName,
                            dateOfBirth: dateOfBirth,
                            gender: gender,
                            profileImageUrl: pictureClaim
                        );

                        var fallbackTempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        var cacheKey = $"oauth_token_{fallbackTempToken}";
                        _logger.LogInformation("[DEBUG] GoogleCallback:creating-token-cache cacheKey={CacheKey} hasAccessToken={HasAccessToken} isNewUser={IsNewUser} userAlreadyExists={UserAlreadyExists}", 
                            cacheKey,
                            !string.IsNullOrEmpty(authResponse.Response.AccessToken),
                            authResponse.IsNewUser,
                            !authResponse.IsNewUser);
                        _memoryCache.Set(cacheKey, new OAuthTokenCache
                        {
                            AccessToken = authResponse.Response.AccessToken,
                            RefreshToken = authResponse.Response.RefreshToken,
                            ExpiresAt = authResponse.Response.ExpiresAt,
                            IsNewUser = authResponse.IsNewUser,
                            UserAlreadyExists = !authResponse.IsNewUser
                        }, TimeSpan.FromMinutes(10)); // Increased from 5 to 10 minutes to handle delays

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

                var claims = result.Principal?.Claims.ToList() ?? new List<System.Security.Claims.Claim>();
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
                    _logger.LogWarning("Meta authentication: missing email or ID");
                    return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Invalid Meta authentication data")}");
                }

                // Parse Meta birthday (format: MM/DD/YYYY or MM/DD)
                DateTime? dateOfBirth = null;
                if (!string.IsNullOrEmpty(birthdayClaim))
                {
                    // Meta provides birthday in MM/DD/YYYY or MM/DD format
                    var parts = birthdayClaim.Split('/');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var day))
                    {
                        var year = parts.Length == 3 && int.TryParse(parts[2], out var y) ? y : DateTime.Now.Year - 18; // Default to 18 years ago if year not provided
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
                    // Map Meta gender to our enum (male/female/other)
                    gender = genderClaim.ToLower() switch
                    {
                        "male" => "Male",
                        "female" => "Female",
                        _ => "Other"
                    };
                }

                _logger.LogInformation("Meta authentication successful for email: {Email}", email);

                var authResponse = await _authService.CreateOrUpdateOAuthUserAsync(
                    email: email,
                    providerId: metaId,
                    provider: AuthProvider.Meta,
                    firstName: firstName,
                    lastName: lastName,
                    dateOfBirth: dateOfBirth,
                    gender: gender,
                    profileImageUrl: pictureClaim
                );

                var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                
                var cacheKey = $"oauth_token_{tempToken}";
                _logger.LogInformation("[DEBUG] MetaCallback:creating-token-cache cacheKey={CacheKey} hasAccessToken={HasAccessToken} isNewUser={IsNewUser} userAlreadyExists={UserAlreadyExists}", 
                    cacheKey,
                    !string.IsNullOrEmpty(authResponse.Response.AccessToken),
                    authResponse.IsNewUser,
                    !authResponse.IsNewUser);
                _memoryCache.Set(cacheKey, new OAuthTokenCache
                {
                    AccessToken = authResponse.Response.AccessToken,
                    RefreshToken = authResponse.Response.RefreshToken,
                    ExpiresAt = authResponse.Response.ExpiresAt,
                    IsNewUser = authResponse.IsNewUser,
                    UserAlreadyExists = !authResponse.IsNewUser
                }, TimeSpan.FromMinutes(10)); // Increased from 5 to 10 minutes to handle delays

                return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Meta OAuth callback");
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                return Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Error processing Meta authentication")}");
            }
        }

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
                _logger.LogInformation("[DEBUG] ExchangeToken:entry codeLength={CodeLength} codePreview={CodePreview} cacheKey={CacheKey}", 
                    request.Code?.Length ?? 0, 
                    request.Code != null ? request.Code.Substring(0, Math.Min(20, request.Code.Length)) : "null",
                    cacheKey);
                
                if (!_memoryCache.TryGetValue(cacheKey, out OAuthTokenCache? cachedData) || cachedData == null)
                {
                    _logger.LogWarning("[DEBUG] ExchangeToken:cache-miss codeLength={CodeLength} cacheKey={CacheKey} cacheExists={CacheExists}", 
                        request.Code?.Length ?? 0, 
                        cacheKey,
                        _memoryCache.TryGetValue(cacheKey, out _));
                    _logger.LogWarning("Invalid or expired OAuth exchange token");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_TOKEN_EXPIRED",
                            Message = "Invalid or expired token. Please try logging in again.",
                            Details = new Dictionary<string, object>
                            {
                                { "reason", "OAuth exchange code expired or not found in cache" },
                                { "codeLength", request.Code?.Length ?? 0 },
                                { "cacheKey", cacheKey }
                            }
                        }
                    });
                }
                
                _logger.LogInformation("[DEBUG] ExchangeToken:cache-hit hasAccessToken={HasAccessToken} hasRefreshToken={HasRefreshToken} isNewUser={IsNewUser} userAlreadyExists={UserAlreadyExists}", 
                    !string.IsNullOrEmpty(cachedData.AccessToken),
                    !string.IsNullOrEmpty(cachedData.RefreshToken),
                    cachedData.IsNewUser,
                    cachedData.UserAlreadyExists);
                
                _logger.LogInformation("Successfully retrieved tokens from cache for code");

                _memoryCache.Remove(cacheKey);

                // Return wrapped in ApiResponse for consistency with other endpoints
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        accessToken = cachedData.AccessToken,
                        refreshToken = cachedData.RefreshToken,
                        expiresAt = cachedData.ExpiresAt,
                        isNewUser = cachedData.IsNewUser,
                        userAlreadyExists = cachedData.UserAlreadyExists
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging OAuth token");
                return StatusCode(500, new { error = "Error exchanging token" });
            }
        }
    }
}

