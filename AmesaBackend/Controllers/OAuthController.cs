using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AmesaBackend.Services;
using System.Security.Claims;
using System.Text.Json;

namespace AmesaBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<OAuthController> _logger;
        private readonly string _frontendUrl;

        public OAuthController(
            IAuthService authService,
            IUserService userService,
            ILogger<OAuthController> logger,
            IConfiguration configuration)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
            _frontendUrl = configuration["Authentication:FrontendUrl"] ?? "http://localhost:4200";
        }

        /// <summary>
        /// Initiate Google OAuth login
        /// </summary>
        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            try
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(GoogleCallback)),
                    Items =
                    {
                        { "scheme", "Google" }
                    }
                };

                _logger.LogInformation("Initiating Google OAuth login");
                return Challenge(properties, "Google");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google OAuth");
                return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Failed to initiate Google login")}");
            }
        }

        /// <summary>
        /// Handle Google OAuth callback
        /// </summary>
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync("Google");

                if (!result.Succeeded || result.Principal == null)
                {
                    _logger.LogWarning("Google authentication failed");
                    return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Google authentication failed")}");
                }

                var claims = result.Principal.Claims;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                {
                    _logger.LogWarning("Google authentication: missing email or ID");
                    return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Invalid Google authentication data")}");
                }

                _logger.LogInformation("Google authentication successful for email: {Email}", email);

                // Find or create user
                var user = await _userService.FindOrCreateOAuthUserAsync(
                    email: email,
                    name: name ?? email,
                    provider: Models.AuthProvider.Google,
                    providerId: googleId
                );

                // Generate JWT tokens
                var token = await _userService.GenerateJwtTokenAsync(user);
                var refreshToken = await _userService.GenerateRefreshTokenAsync(user);

                // Prepare user data for frontend
                var userData = new
                {
                    id = user.Id,
                    email = user.Email,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    profileImageUrl = user.ProfileImageUrl
                };

                var userJson = Uri.EscapeDataString(JsonSerializer.Serialize(userData));
                var redirectUrl = $"{_frontendUrl}/auth/callback?token={token}&refreshToken={refreshToken}&user={userJson}";

                _logger.LogInformation("Redirecting to frontend with auth data for user: {UserId}", user.Id);
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google OAuth callback");
                return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Authentication processing failed")}");
            }
        }

        /// <summary>
        /// Initiate Facebook OAuth login
        /// </summary>
        [HttpGet("facebook")]
        public IActionResult FacebookLogin()
        {
            try
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(FacebookCallback)),
                    Items =
                    {
                        { "scheme", "Facebook" }
                    }
                };

                _logger.LogInformation("Initiating Facebook OAuth login");
                return Challenge(properties, "Facebook");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Facebook OAuth");
                return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Failed to initiate Facebook login")}");
            }
        }

        /// <summary>
        /// Handle Facebook OAuth callback
        /// </summary>
        [HttpGet("facebook-callback")]
        public async Task<IActionResult> FacebookCallback()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync("Facebook");

                if (!result.Succeeded || result.Principal == null)
                {
                    _logger.LogWarning("Facebook authentication failed");
                    return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Facebook authentication failed")}");
                }

                var claims = result.Principal.Claims;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var facebookId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(facebookId))
                {
                    _logger.LogWarning("Facebook authentication: missing email or ID");
                    return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Invalid Facebook authentication data")}");
                }

                _logger.LogInformation("Facebook authentication successful for email: {Email}", email);

                // Find or create user
                var user = await _userService.FindOrCreateOAuthUserAsync(
                    email: email,
                    name: name ?? email,
                    provider: Models.AuthProvider.Meta,
                    providerId: facebookId
                );

                // Generate JWT tokens
                var token = await _userService.GenerateJwtTokenAsync(user);
                var refreshToken = await _userService.GenerateRefreshTokenAsync(user);

                // Prepare user data for frontend
                var userData = new
                {
                    id = user.Id,
                    email = user.Email,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    profileImageUrl = user.ProfileImageUrl
                };

                var userJson = Uri.EscapeDataString(JsonSerializer.Serialize(userData));
                var redirectUrl = $"{_frontendUrl}/auth/callback?token={token}&refreshToken={refreshToken}&user={userJson}";

                _logger.LogInformation("Redirecting to frontend with auth data for user: {UserId}", user.Id);
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Facebook OAuth callback");
                return Redirect($"{_frontendUrl}/auth/callback?error={Uri.EscapeDataString("Authentication processing failed")}");
            }
        }
    }
}

