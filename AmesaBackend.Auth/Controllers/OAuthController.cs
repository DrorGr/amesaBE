using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text.Json;

namespace AmesaBackend.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthController> _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly IRateLimitService _rateLimitService;
        private readonly JsonSerializerOptions _jsonOptions;

        public OAuthController(
            IAuthService authService,
            IConfiguration configuration,
            ILogger<OAuthController> logger,
            IDistributedCache distributedCache,
            IRateLimitService rateLimitService)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
            _distributedCache = distributedCache;
            _rateLimitService = rateLimitService;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        /// <summary>
        /// Builds an error redirect URL with error code and optional details
        /// </summary>
        private string BuildErrorRedirectUrl(string errorCode, string? provider = null, Dictionary<string, object>? details = null)
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
            var errorParams = new List<string> { $"error_code={Uri.EscapeDataString(errorCode)}" };
            
            if (!string.IsNullOrEmpty(provider))
            {
                errorParams.Add($"provider={Uri.EscapeDataString(provider)}");
            }
            
            if (details != null && details.Count > 0)
            {
                // Encode details as JSON and base64 for URL safety
                var detailsJson = JsonSerializer.Serialize(details, _jsonOptions);
                var detailsBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(detailsJson))
                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');
                errorParams.Add($"details={Uri.EscapeDataString(detailsBase64)}");
            }
            
            return $"{frontendUrl}/auth/callback?{string.Join("&", errorParams)}";
        }

        [HttpGet("google")]
        public async Task<IActionResult> GoogleLogin()
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
                                { "missing", string.IsNullOrWhiteSpace(googleClientId) ? "ClientId" : "ClientSecret" },
                                { "secretId", _configuration["Authentication:Google:SecretId"] ?? "amesa-google_people_API" }
                            }
                        }
                    });
                }

                // Verify that the authentication scheme is registered
                // If AddGoogleOAuth didn't register the scheme (e.g., due to missing credentials at startup),
                // Challenge() will throw an InvalidOperationException
                var authSchemeProvider = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                var googleScheme = await authSchemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme);
                
                if (googleScheme == null)
                {
                    _logger.LogError("Google OAuth authentication scheme is not registered. This usually means credentials were not loaded at startup.");
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_SCHEME_NOT_REGISTERED",
                            Message = "Google OAuth authentication scheme is not registered. Please verify AWS Secrets Manager secret 'amesa-google_people_API' exists and contains valid ClientId and ClientSecret.",
                            Details = new Dictionary<string, object>
                            {
                                { "provider", "Google" },
                                { "secretId", _configuration["Authentication:Google:SecretId"] ?? "amesa-google_people_API" },
                                { "hasClientId", !string.IsNullOrWhiteSpace(googleClientId) },
                                { "hasClientSecret", !string.IsNullOrWhiteSpace(googleClientSecret) }
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("No authenticationScheme", StringComparison.OrdinalIgnoreCase) || 
                                                         ex.Message.Contains("authentication scheme", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "Google OAuth authentication scheme not registered. Exception: {ExceptionType}, Message: {Message}", 
                    ex.GetType().Name, ex.Message);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "OAUTH_SCHEME_NOT_REGISTERED",
                        Message = "Google OAuth authentication scheme is not registered. Please verify AWS Secrets Manager secret 'amesa-google_people_API' exists and contains valid ClientId and ClientSecret.",
                        Details = new Dictionary<string, object>
                        {
                            { "provider", "Google" },
                            { "secretId", _configuration["Authentication:Google:SecretId"] ?? "amesa-google_people_API" },
                            { "exception_type", ex.GetType().Name },
                            { "exception_message", ex.Message }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google OAuth. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "OAUTH_INIT_FAILED",
                        Message = "An error occurred while initiating Google OAuth login.",
                        Details = new Dictionary<string, object>
                        {
                            { "provider", "Google" },
                            { "exception_type", ex.GetType().Name },
                            { "exception_message", ex.Message }
                        }
                    }
                });
            }
        }

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                // Rate limiting for OAuth callback endpoint
                var clientIp = HttpContext.Items["ClientIp"]?.ToString() 
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? HttpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                    ?? "unknown";
                var rateLimitKey = $"oauth:google-callback:{clientIp}";
                
                // Atomically increment and check rate limit to prevent race conditions
                // Allow maximum 10 OAuth callback attempts per 15 minutes per IP
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 10, TimeSpan.FromMinutes(15));
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
                
                if (!isAllowed)
                {
                    _logger.LogWarning("OAuth callback rate limit exceeded for IP: {ClientIp}", clientIp);
                    var errorFrontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                    return Redirect($"{errorFrontendUrl}/auth/callback?error={Uri.EscapeDataString("Too many authentication attempts. Please try again later.")}");
                }

                var googleResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!googleResult.Succeeded)
                {
                    var failureReason = googleResult.Failure?.Message ?? "Unknown";
                    _logger.LogWarning("Google OAuth callback: Authentication failed. Reason: {Reason}, Failure: {Failure}", 
                        failureReason, googleResult.Failure?.ToString() ?? "None");
                    return Redirect(BuildErrorRedirectUrl("OAUTH_AUTHENTICATION_FAILED", "Google", new Dictionary<string, object>
                    {
                        { "reason", failureReason }
                    }));
                }

                // Get email from authenticated principal (used in multiple places)
                var email = googleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                // Try to get temp_token from authentication properties (set in OnCreatingTicket)
                var tempToken = googleResult.Properties?.Items.TryGetValue("temp_token", out var token) == true 
                    ? token 
                    : Request.Query["temp_token"].FirstOrDefault();
                
                // If not found in properties, try to get it from email cache (fallback)
                if (string.IsNullOrEmpty(tempToken) && !string.IsNullOrEmpty(email))
                {
                    // Use hashed email for cache key to protect privacy
                    var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())));
                    var emailCacheKey = $"oauth_temp_token_{emailHash}";
                    var cachedTokenBytes = await _distributedCache.GetAsync(emailCacheKey);
                    if (cachedTokenBytes != null && cachedTokenBytes.Length > 0)
                    {
                        tempToken = System.Text.Encoding.UTF8.GetString(cachedTokenBytes);
                        _logger.LogInformation("Google OAuth callback: Found temp_token from email cache");
                        // Remove from cache after use
                        await _distributedCache.RemoveAsync(emailCacheKey);
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
                        var cacheExpiration = TimeSpan.FromMinutes(
                            _configuration.GetValue<int>("SecuritySettings:OAuthTokenCacheExpirationMinutes", 5));
                        var cacheData = new OAuthTokenCache
                        {
                            AccessToken = authResponse.Response.AccessToken,
                            RefreshToken = authResponse.Response.RefreshToken,
                            ExpiresAt = authResponse.Response.ExpiresAt,
                            IsNewUser = authResponse.IsNewUser,
                            UserAlreadyExists = !authResponse.IsNewUser
                        };
                        var cacheJson = JsonSerializer.Serialize(cacheData, _jsonOptions);
                        var cacheBytes = System.Text.Encoding.UTF8.GetBytes(cacheJson);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = cacheExpiration
                        };
                        await _distributedCache.SetAsync(cacheKey, cacheBytes, cacheOptions);

                        await HttpContext.SignOutAsync("Cookies");
                        await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                        return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(fallbackTempToken)}");
                    }
                }

                _logger.LogWarning("Google OAuth callback: Could not process callback - missing required data (email or googleId)");
                await HttpContext.SignOutAsync("Cookies");
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                return Redirect(BuildErrorRedirectUrl("OAUTH_MISSING_DATA", "Google", new Dictionary<string, object>
                {
                    { "missing", "email or googleId" }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google OAuth callback. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                await HttpContext.SignOutAsync("Cookies");
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                return Redirect(BuildErrorRedirectUrl("OAUTH_PROCESSING_ERROR", "Google", new Dictionary<string, object>
                {
                    { "exception_type", ex.GetType().Name }
                }));
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
                _logger.LogError(ex, "Error initiating Meta OAuth. Exception: {ExceptionType}, Message: {Message}", 
                    ex.GetType().Name, ex.Message);
                return Redirect(BuildErrorRedirectUrl("OAUTH_INIT_FAILED", "Meta", new Dictionary<string, object>
                {
                    { "exception_type", ex.GetType().Name }
                }));
            }
        }

        [HttpGet("meta-callback")]
        public async Task<IActionResult> MetaCallback()
        {
            try
            {
                // Rate limiting for OAuth callback endpoint
                var clientIp = HttpContext.Items["ClientIp"]?.ToString() 
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? HttpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                    ?? "unknown";
                var rateLimitKey = $"oauth:meta-callback:{clientIp}";
                
                // Atomically increment and check rate limit to prevent race conditions
                // Allow maximum 10 OAuth callback attempts per 15 minutes per IP
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 10, TimeSpan.FromMinutes(15));
                var frontendUrl = _configuration["FrontendUrl"] ?? 
                                 _configuration.GetSection("AllowedOrigins").Get<string[]>()?[0] ?? 
                                 "https://dpqbvdgnenckf.cloudfront.net";
                
                if (!isAllowed)
                {
                    _logger.LogWarning("OAuth callback rate limit exceeded for IP: {ClientIp}, Provider: Meta", clientIp);
                    return Redirect(BuildErrorRedirectUrl("OAUTH_RATE_LIMIT_EXCEEDED", "Meta", new Dictionary<string, object>
                    {
                        { "ip", clientIp },
                        { "retry_after_minutes", 15 }
                    }));
                }

                var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

                if (!result.Succeeded)
                {
                    var failureReason = result.Failure?.Message ?? "Unknown";
                    _logger.LogWarning("Meta OAuth callback: Authentication failed. Reason: {Reason}, Failure: {Failure}", 
                        failureReason, result.Failure?.ToString() ?? "None");
                    return Redirect(BuildErrorRedirectUrl("OAUTH_AUTHENTICATION_FAILED", "Meta", new Dictionary<string, object>
                    {
                        { "reason", failureReason }
                    }));
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
                    _logger.LogWarning("Meta OAuth callback: Missing required data. Email: {HasEmail}, MetaId: {HasMetaId}", 
                        !string.IsNullOrEmpty(email), !string.IsNullOrEmpty(metaId));
                    return Redirect(BuildErrorRedirectUrl("OAUTH_MISSING_DATA", "Meta", new Dictionary<string, object>
                    {
                        { "missing", string.IsNullOrEmpty(email) ? "email" : "metaId" }
                    }));
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
                var cacheExpiration = TimeSpan.FromMinutes(
                    _configuration.GetValue<int>("SecuritySettings:OAuthTokenCacheExpirationMinutes", 5));
                var cacheData = new OAuthTokenCache
                {
                    AccessToken = authResponse.Response.AccessToken,
                    RefreshToken = authResponse.Response.RefreshToken,
                    ExpiresAt = authResponse.Response.ExpiresAt,
                    IsNewUser = authResponse.IsNewUser,
                    UserAlreadyExists = !authResponse.IsNewUser
                };
                var cacheJson = JsonSerializer.Serialize(cacheData, _jsonOptions);
                var cacheBytes = System.Text.Encoding.UTF8.GetBytes(cacheJson);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration
                };
                await _distributedCache.SetAsync(cacheKey, cacheBytes, cacheOptions);

                return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Meta OAuth callback. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                await HttpContext.SignOutAsync("Cookies");
                await HttpContext.SignOutAsync(FacebookDefaults.AuthenticationScheme);
                return Redirect(BuildErrorRedirectUrl("OAUTH_PROCESSING_ERROR", "Meta", new Dictionary<string, object>
                {
                    { "exception_type", ex.GetType().Name }
                }));
            }
        }

        [HttpGet("apple")]
        public async Task<IActionResult> AppleLogin()
        {
            try
            {
                var frontendUrl = _configuration["FrontendUrl"] ?? 
                                 _configuration.GetSection("AllowedOrigins").Get<string[]>()?[0] ?? 
                                 "https://dpqbvdgnenckf.cloudfront.net";

                // Check if Apple OAuth is configured
                var appleClientId = _configuration["Authentication:Apple:ClientId"];
                var appleTeamId = _configuration["Authentication:Apple:TeamId"];
                var appleKeyId = _configuration["Authentication:Apple:KeyId"];
                var applePrivateKey = _configuration["Authentication:Apple:PrivateKey"];
                
                if (string.IsNullOrWhiteSpace(appleClientId) || 
                    string.IsNullOrWhiteSpace(appleTeamId) || 
                    string.IsNullOrWhiteSpace(appleKeyId) || 
                    string.IsNullOrWhiteSpace(applePrivateKey))
                {
                    _logger.LogWarning("Apple OAuth not configured - missing required credentials");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_NOT_CONFIGURED",
                            Message = "Apple OAuth is not configured. Please configure ClientId, TeamId, KeyId, and PrivateKey in appsettings.json or AWS Secrets Manager.",
                            Details = new Dictionary<string, object>
                            {
                                { "provider", "Apple" },
                                { "missing", GetMissingAppleConfig(appleClientId, appleTeamId, appleKeyId, applePrivateKey) },
                                { "secretId", _configuration["Authentication:Apple:SecretId"] ?? "amesa-apple-oauth" }
                            }
                        }
                    });
                }

                // Verify that the authentication scheme is registered
                var authSchemeProvider = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                var appleScheme = await authSchemeProvider.GetSchemeAsync(AppleAuthenticationDefaults.AuthenticationScheme);
                
                if (appleScheme == null)
                {
                    _logger.LogError("Apple OAuth authentication scheme is not registered. This usually means credentials were not loaded at startup.");
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_SCHEME_NOT_REGISTERED",
                            Message = "Apple OAuth authentication scheme is not registered. Please verify AWS Secrets Manager secret 'amesa-apple-oauth' exists and contains valid credentials.",
                            Details = new Dictionary<string, object>
                            {
                                { "provider", "Apple" },
                                { "secretId", _configuration["Authentication:Apple:SecretId"] ?? "amesa-apple-oauth" },
                                { "hasClientId", !string.IsNullOrWhiteSpace(appleClientId) },
                                { "hasTeamId", !string.IsNullOrWhiteSpace(appleTeamId) },
                                { "hasKeyId", !string.IsNullOrWhiteSpace(appleKeyId) },
                                { "hasPrivateKey", !string.IsNullOrWhiteSpace(applePrivateKey) }
                            }
                        }
                    });
                }

                _logger.LogInformation("Initiating Apple OAuth login");
                
                var properties = new AuthenticationProperties
                {
                    RedirectUri = $"{frontendUrl}/auth/callback",
                    AllowRefresh = true
                };
                
                return Challenge(properties, AppleAuthenticationDefaults.AuthenticationScheme);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No authenticationScheme", StringComparison.OrdinalIgnoreCase) || 
                                                         ex.Message.Contains("authentication scheme", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "Apple OAuth authentication scheme not registered. Exception: {ExceptionType}, Message: {Message}", 
                    ex.GetType().Name, ex.Message);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "OAUTH_SCHEME_NOT_REGISTERED",
                        Message = "Apple OAuth authentication scheme is not registered. Please verify AWS Secrets Manager secret 'amesa-apple-oauth' exists and contains valid credentials.",
                        Details = new Dictionary<string, object>
                        {
                            { "provider", "Apple" },
                            { "secretId", _configuration["Authentication:Apple:SecretId"] ?? "amesa-apple-oauth" },
                            { "exception_type", ex.GetType().Name },
                            { "exception_message", ex.Message }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Apple OAuth. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "OAUTH_INIT_FAILED",
                        Message = "An error occurred while initiating Apple OAuth login.",
                        Details = new Dictionary<string, object>
                        {
                            { "provider", "Apple" },
                            { "exception_type", ex.GetType().Name },
                            { "exception_message", ex.Message }
                        }
                    }
                });
            }
        }

        [HttpGet("apple-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> AppleCallback()
        {
            try
            {
                // Rate limiting for OAuth callback endpoint
                var clientIp = HttpContext.Items["ClientIp"]?.ToString() 
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? HttpContext.Request.Headers["X-Real-Ip"].FirstOrDefault()
                    ?? "unknown";
                var rateLimitKey = $"oauth:apple-callback:{clientIp}";
                
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 10, TimeSpan.FromMinutes(15));
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
                
                if (!isAllowed)
                {
                    _logger.LogWarning("OAuth callback rate limit exceeded for IP: {ClientIp}, Provider: Apple", clientIp);
                    return Redirect(BuildErrorRedirectUrl("OAUTH_RATE_LIMIT_EXCEEDED", "Apple", new Dictionary<string, object>
                    {
                        { "ip", clientIp },
                        { "retry_after_minutes", 15 }
                    }));
                }

                var appleResult = await HttpContext.AuthenticateAsync(AppleAuthenticationDefaults.AuthenticationScheme);
                
                if (!appleResult.Succeeded)
                {
                    var failureReason = appleResult.Failure?.Message ?? "Unknown";
                    _logger.LogWarning("Apple OAuth callback: Authentication failed. Reason: {Reason}, Failure: {Failure}", 
                        failureReason, appleResult.Failure?.ToString() ?? "None");
                    return Redirect(BuildErrorRedirectUrl("OAUTH_AUTHENTICATION_FAILED", "Apple", new Dictionary<string, object>
                    {
                        { "reason", failureReason }
                    }));
                }

                var email = appleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var tempToken = appleResult.Properties?.Items.TryGetValue("temp_token", out var token) == true 
                    ? token 
                    : Request.Query["temp_token"].FirstOrDefault();
                
                // If not found in properties, try to get it from email cache (fallback)
                if (string.IsNullOrEmpty(tempToken) && !string.IsNullOrEmpty(email))
                {
                    var emailHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())));
                    var emailCacheKey = $"oauth_temp_token_{emailHash}";
                    var cachedTokenBytes = await _distributedCache.GetAsync(emailCacheKey);
                    if (cachedTokenBytes != null && cachedTokenBytes.Length > 0)
                    {
                        tempToken = System.Text.Encoding.UTF8.GetString(cachedTokenBytes);
                        _logger.LogInformation("Apple OAuth callback: Found temp_token from email cache");
                        await _distributedCache.RemoveAsync(emailCacheKey);
                    }
                }
                
                if (!string.IsNullOrEmpty(tempToken))
                {
                    _logger.LogInformation("Apple OAuth callback: Found temp_token, redirecting to frontend with code");
                    await HttpContext.SignOutAsync("Cookies");
                    await HttpContext.SignOutAsync(AppleAuthenticationDefaults.AuthenticationScheme);
                    return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}");
                }
                
                _logger.LogWarning("Apple OAuth callback: temp_token not found, using fallback");
                if (!string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Fallback: Processing Apple OAuth callback for: {Email}", email);
                    var appleId = appleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var firstName = appleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
                    var lastName = appleResult.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;

                    if (!string.IsNullOrEmpty(appleId))
                    {
                        var authResponse = await _authService.CreateOrUpdateOAuthUserAsync(
                            email: email,
                            providerId: appleId,
                            provider: AuthProvider.Apple,
                            firstName: firstName,
                            lastName: lastName,
                            dateOfBirth: null,
                            gender: null,
                            profileImageUrl: null
                        );

                        var fallbackTempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        var cacheKey = $"oauth_token_{fallbackTempToken}";
                        
                        var cacheExpiration = TimeSpan.FromMinutes(
                            _configuration.GetValue<int>("SecuritySettings:OAuthTokenCacheExpirationMinutes", 5));
                        var cacheData = new OAuthTokenCache
                        {
                            AccessToken = authResponse.Response.AccessToken,
                            RefreshToken = authResponse.Response.RefreshToken,
                            ExpiresAt = authResponse.Response.ExpiresAt,
                            IsNewUser = authResponse.IsNewUser,
                            UserAlreadyExists = !authResponse.IsNewUser
                        };
                        var cacheJson = JsonSerializer.Serialize(cacheData, _jsonOptions);
                        var cacheBytes = System.Text.Encoding.UTF8.GetBytes(cacheJson);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = cacheExpiration
                        };
                        await _distributedCache.SetAsync(cacheKey, cacheBytes, cacheOptions);

                        await HttpContext.SignOutAsync("Cookies");
                        await HttpContext.SignOutAsync(AppleAuthenticationDefaults.AuthenticationScheme);
                        return Redirect($"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(fallbackTempToken)}");
                    }
                }

                _logger.LogWarning("Apple OAuth callback: Could not process callback - missing required data (email or appleId)");
                await HttpContext.SignOutAsync("Cookies");
                await HttpContext.SignOutAsync(AppleAuthenticationDefaults.AuthenticationScheme);
                return Redirect(BuildErrorRedirectUrl("OAUTH_MISSING_DATA", "Apple", new Dictionary<string, object>
                {
                    { "missing", "email or appleId" }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Apple OAuth callback. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                await HttpContext.SignOutAsync("Cookies");
                await HttpContext.SignOutAsync(AppleAuthenticationDefaults.AuthenticationScheme);
                return Redirect(BuildErrorRedirectUrl("OAUTH_PROCESSING_ERROR", "Apple", new Dictionary<string, object>
                {
                    { "exception_type", ex.GetType().Name }
                }));
            }
        }

        private string GetMissingAppleConfig(string? clientId, string? teamId, string? keyId, string? privateKey)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(clientId)) missing.Add("ClientId");
            if (string.IsNullOrWhiteSpace(teamId)) missing.Add("TeamId");
            if (string.IsNullOrWhiteSpace(keyId)) missing.Add("KeyId");
            if (string.IsNullOrWhiteSpace(privateKey)) missing.Add("PrivateKey");
            return string.Join(", ", missing);
        }

        [HttpPost("exchange")]
        [AllowAnonymous]
        public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new { error = "Code is required" });
                }

                var cacheKey = $"oauth_token_{request.Code}";
                
                var cachedDataBytes = await _distributedCache.GetAsync(cacheKey);
                OAuthTokenCache? cachedData = null;
                
                if (cachedDataBytes == null || cachedDataBytes.Length == 0)
                {
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
                
                // Deserialize cached data
                var cachedJson = System.Text.Encoding.UTF8.GetString(cachedDataBytes);
                cachedData = JsonSerializer.Deserialize<OAuthTokenCache>(cachedJson, _jsonOptions);
                
                if (cachedData == null)
                {
                    _logger.LogWarning("Failed to deserialize OAuth token cache data");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "OAUTH_TOKEN_EXPIRED",
                            Message = "Invalid or expired token. Please try logging in again."
                        }
                    });
                }
                
                _logger.LogInformation("Successfully retrieved tokens from cache for code");

                await _distributedCache.RemoveAsync(cacheKey);

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

