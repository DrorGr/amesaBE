using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Shared.Helpers;
using System.Text.Json;

namespace AmesaBackend.Auth.Controllers
{
    /// <summary>
    /// Controller for handling authentication operations including registration, login, and token management.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICaptchaService _captchaService;
        private readonly IRateLimitService _rateLimitService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IAccountRecoveryService _accountRecoveryService;
        private readonly IAccountDeletionService _accountDeletionService;
        private readonly AuthDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="captchaService">The CAPTCHA verification service.</param>
        /// <param name="rateLimitService">The rate limiting service.</param>
        /// <param name="context">The database context.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="logger">The logger instance.</param>
        public AuthController(
            IAuthService authService,
            ICaptchaService captchaService,
            IRateLimitService rateLimitService,
            ITwoFactorService twoFactorService,
            IAccountRecoveryService accountRecoveryService,
            IAccountDeletionService accountDeletionService,
            AuthDbContext context,
            IDistributedCache cache,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _captchaService = captchaService;
            _rateLimitService = rateLimitService;
            _twoFactorService = twoFactorService;
            _accountRecoveryService = accountRecoveryService;
            _accountDeletionService = accountDeletionService;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account with email verification and CAPTCHA protection.
        /// </summary>
        /// <param name="request">The registration request containing user details and CAPTCHA token.</param>
        /// <returns>An authentication response with user details if successful, or an error response.</returns>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Rate limiting - use IP from middleware if available
                var clientIp = HttpContext.Items["ClientIp"]?.ToString() 
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? "unknown";
                var rateLimitKey = $"registration:{clientIp}";
                
                // Atomically increment and check rate limit to prevent race conditions
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 5, TimeSpan.FromHours(1));
                
                if (!isAllowed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many registration attempts. Please try again later."
                        }
                    });
                }

                // CAPTCHA verification
                if (string.IsNullOrWhiteSpace(request.CaptchaToken) || 
                    !await _captchaService.VerifyCaptchaAsync(request.CaptchaToken, "register"))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "CAPTCHA_FAILED",
                            Message = "CAPTCHA verification failed"
                        }
                    });
                }

                var result = await _authService.RegisterAsync(request);
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Registration successful. Please check your email to verify your account."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during registration"
                    }
                });
            }
        }

        /// <summary>
        /// Authenticates a user and returns access and refresh tokens.
        /// </summary>
        /// <param name="request">The login request containing email and password.</param>
        /// <returns>An authentication response with tokens if successful, or an error response.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Rate limiting by email - atomically increment and check to prevent race conditions
                var rateLimitKey = $"login:{request.Email}";
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 5, TimeSpan.FromMinutes(15));
                
                if (!isAllowed)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many login attempts. Please try again later."
                        }
                    });
                }

                var result = await _authService.LoginAsync(request);
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Login successful"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Use generic error message to prevent email enumeration
                // Don't reveal if user exists or if password is wrong
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "AUTHENTICATION_ERROR",
                        Message = "Invalid email or password" // Generic message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during login"
                    }
                });
            }
        }

        /// <summary>
        /// Refreshes an access token using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token request containing the refresh token.</param>
        /// <returns>An authentication response with new tokens if successful, or an error response.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request);
                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Token refreshed successfully"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "AUTHENTICATION_ERROR",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during token refresh"
                    }
                });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                await _authService.LogoutAsync(request.RefreshToken);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during logout"
                    }
                });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                // Rate limiting: 3 requests per hour per email
                var rateLimitKey = $"password-reset:{request.Email}";
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 3, TimeSpan.FromHours(1));
                
                if (!isAllowed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many password reset requests. Please try again later."
                        }
                    });
                }

                await _authService.ForgotPasswordAsync(request);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link has been sent"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while processing your request"
                    }
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Rate limiting: 5 attempts per hour per token (to prevent brute force on tokens)
                var tokenRateLimitKey = $"password-reset-attempt:{request.Token}";
                var isTokenAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(tokenRateLimitKey, 5, TimeSpan.FromHours(1));
                
                if (!isTokenAllowed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many password reset attempts. Please request a new password reset link."
                        }
                    });
                }

                // Additional rate limiting: 10 attempts per hour per IP (to prevent distributed attacks)
                var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var ipRateLimitKey = $"password-reset-ip:{clientIp}";
                var isIpAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(ipRateLimitKey, 10, TimeSpan.FromHours(1));
                
                if (!isIpAllowed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many password reset attempts from this IP. Please try again later."
                        }
                    });
                }

                await _authService.ResetPasswordAsync(request);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password reset successful"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during password reset"
                    }
                });
            }
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                await _authService.VerifyEmailAsync(request);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Email verified successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during email verification"
                    }
                });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<ActionResult<ApiResponse<object>>> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
        {
            try
            {
                // Rate limiting for resend verification - atomically increment and check to prevent race conditions
                var rateLimitKey = $"resend-verification:{request.Email}";
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 3, TimeSpan.FromHours(1));
                
                if (!isAllowed)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many verification email requests. Please try again later."
                        }
                    });
                }
                await _authService.ResendVerificationEmailAsync(request);
                
                // Always return success to prevent email enumeration
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If the email exists and is not verified, a verification email has been sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                // Return success even on error to prevent email enumeration
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If the email exists and is not verified, a verification email has been sent."
                });
            }
        }

        [HttpPost("verify-phone")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> VerifyPhone([FromBody] VerifyPhoneRequest request)
        {
            try
            {
                await _authService.VerifyPhoneAsync(request);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Phone verified successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during phone verification");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred during phone verification"
                    }
                });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                var user = await _authService.GetCurrentUserAsync(userId);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving user information"
                    }
                });
            }
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateUserProfileRequest request)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                var userService = HttpContext.RequestServices.GetRequiredService<IUserService>();
                var updatedUser = await userService.UpdateUserProfileAsync(userId, request);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = updatedUser,
                    Message = "Profile updated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "USER_NOT_FOUND",
                        Message = ex.Message
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                // Handle profile locked after verification
                if (ex.Message.Contains("PROFILE_LOCKED_AFTER_VERIFICATION"))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "PROFILE_LOCKED_AFTER_VERIFICATION",
                            Message = ex.Message
                        }
                    });
                }
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while updating profile"
                    }
                });
            }
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetActiveSessions()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                var sessions = await _authService.GetActiveSessionsAsync(userId);
                return Ok(new ApiResponse<List<UserSessionDto>>
                {
                    Success = true,
                    Data = sessions,
                    Message = "Active sessions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active sessions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving sessions"
                    }
                });
            }
        }

        [HttpPost("sessions/{sessionToken}/logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> LogoutFromDevice(string sessionToken)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                await _authService.LogoutFromDeviceAsync(userId, sessionToken);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Logged out from device successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out from device");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while logging out"
                    }
                });
            }
        }

        [HttpPost("sessions/logout-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> LogoutAllDevices()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication required"
                        }
                    });
                }
                await _authService.LogoutAllDevicesAsync(userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Logged out from all devices successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out from all devices");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while logging out"
                    }
                });
            }
        }

        /// <summary>
        /// Checks if a username is available for registration.
        /// Returns availability status and suggestions if the username is taken.
        /// </summary>
        /// <param name="username">The username to check (from query parameter).</param>
        /// <returns>Username availability response with suggestions if taken.</returns>
        [HttpGet("username-availability")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<UsernameAvailabilityResponse>>> CheckUsernameAvailability([FromQuery] string username)
        {
            try
            {
                // Rate limiting - 10 requests per minute per IP
                var clientIp = HttpContext.Items["ClientIp"]?.ToString() 
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? "unknown";
                var rateLimitKey = $"username-check:{clientIp}";
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 10, TimeSpan.FromMinutes(1));
                
                if (!isAllowed)
                {
                    return BadRequest(new ApiResponse<UsernameAvailabilityResponse>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "Too many username availability checks. Please try again later."
                        }
                    });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest(new ApiResponse<UsernameAvailabilityResponse>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "VALIDATION_ERROR",
                            Message = "Username is required"
                        }
                    });
                }

                // Normalize username (trim, lowercase)
                var normalizedUsername = username.Trim().ToLowerInvariant();

                // Validate length
                if (normalizedUsername.Length < 3 || normalizedUsername.Length > 50)
                {
                    return BadRequest(new ApiResponse<UsernameAvailabilityResponse>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "VALIDATION_ERROR",
                            Message = "Username must be between 3 and 50 characters"
                        }
                    });
                }

                // Check cache first (only for "available" results, 5-minute cache)
                var cacheKey = $"username_available:{normalizedUsername}";
                var cachedResult = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedResult))
                {
                    var cached = JsonSerializer.Deserialize<UsernameAvailabilityResponse>(cachedResult);
                    if (cached != null && cached.Available)
                    {
                        _logger.LogDebug("Username availability cache hit for: {Username}", normalizedUsername);
                        return Ok(new ApiResponse<UsernameAvailabilityResponse>
                        {
                            Success = true,
                            Data = cached
                        });
                    }
                }

                // Check database (case-insensitive using the LOWER index)
                // Query uses the idx_users_username_lower index for performance
                var isTaken = await _context.Users
                    .FromSqlRaw("SELECT * FROM amesa_auth.users WHERE LOWER(\"Username\") = {0}", normalizedUsername)
                    .AnyAsync();

                var response = new UsernameAvailabilityResponse
                {
                    Available = !isTaken
                };

                // Generate suggestions if username is taken
                if (isTaken)
                {
                    response.Suggestions = GenerateUsernameSuggestions(normalizedUsername);
                }
                else
                {
                    // Cache "available" result for 5 minutes
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    };
                    var cacheJson = JsonSerializer.Serialize(response);
                    await _cache.SetStringAsync(cacheKey, cacheJson, cacheOptions);
                }

                return Ok(new ApiResponse<UsernameAvailabilityResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability for: {Username}", username);
                return StatusCode(500, new ApiResponse<UsernameAvailabilityResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while checking username availability"
                    }
                });
            }
        }

        /// <summary>
        /// Generates username suggestions when the requested username is taken.
        /// </summary>
        private List<string> GenerateUsernameSuggestions(string baseUsername)
        {
            var suggestions = new List<string>();
            var random = new Random();

            // Generate 3 suggestions
            for (int i = 1; i <= 3; i++)
            {
                string suggestion;
                switch (i)
                {
                    case 1:
                        // Add number suffix
                        suggestion = $"{baseUsername}{random.Next(100, 999)}";
                        break;
                    case 2:
                        // Add underscore and number
                        suggestion = $"{baseUsername}_{random.Next(10, 99)}";
                        break;
                    case 3:
                        // Add year suffix
                        suggestion = $"{baseUsername}{DateTime.UtcNow.Year % 100}";
                        break;
                    default:
                        suggestion = $"{baseUsername}{i}";
                        break;
                }

                // Ensure suggestion is within length limit
                if (suggestion.Length <= 50)
                {
                    suggestions.Add(suggestion);
                }
            }

            return suggestions;
        }

        #region Two-Factor Authentication Endpoints

        /// <summary>
        /// Generates 2FA setup information (QR code and secret).
        /// </summary>
        [HttpPost("two-factor/setup")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<TwoFactorSetupResponse>>> SetupTwoFactor()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "USER_NOT_FOUND", Message = "User not found" }
                    });
                }

                if (user.TwoFactorEnabled)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "TWO_FACTOR_ALREADY_ENABLED", Message = "Two-factor authentication is already enabled" }
                    });
                }

                var (secret, qrCodeImageUrl, manualEntryKey) = await _twoFactorService.GenerateSetupAsync(userId, user.Email);
                return Ok(new ApiResponse<TwoFactorSetupResponse>
                {
                    Success = true,
                    Data = new TwoFactorSetupResponse
                    {
                        Secret = secret,
                        QrCodeImageUrl = qrCodeImageUrl,
                        ManualEntryKey = manualEntryKey
                    },
                    Message = "2FA setup information generated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating 2FA setup");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during 2FA setup" }
                });
            }
        }

        /// <summary>
        /// Verifies the 2FA setup code.
        /// </summary>
        [HttpPost("two-factor/verify-setup")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> VerifySetup([FromBody] VerifyTwoFactorRequest request)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Code is required" }
                    });
                }

                var isValid = await _twoFactorService.VerifySetupCodeAsync(userId, request.Code);
                if (!isValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CODE", Message = "Invalid verification code" }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Setup code verified successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA setup code");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during verification" }
                });
            }
        }

        /// <summary>
        /// Enables 2FA for the current user.
        /// </summary>
        [HttpPost("two-factor/enable")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<string>>>> EnableTwoFactor()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                await _twoFactorService.EnableTwoFactorAsync(userId);
                var backupCodes = await _twoFactorService.GenerateBackupCodesAsync(userId);

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = backupCodes,
                    Message = "Two-factor authentication enabled successfully. Save these backup codes in a safe place."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while enabling 2FA" }
                });
            }
        }

        /// <summary>
        /// Verifies a 2FA code during login.
        /// </summary>
        [HttpPost("two-factor/verify")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyTwoFactor([FromBody] VerifyTwoFactorLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Email and code are required" }
                    });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null || !user.TwoFactorEnabled)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "TWO_FACTOR_NOT_ENABLED", Message = "Two-factor authentication is not enabled for this account" }
                    });
                }

                // Verify password first
                var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                if (!passwordValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "Invalid password" }
                    });
                }

                // Try TOTP code first
                var isValid = await _twoFactorService.VerifyCodeAsync(user.Id, request.Code);
                
                // If TOTP fails, try backup code
                if (!isValid)
                {
                    isValid = await _twoFactorService.VerifyBackupCodeAsync(user.Id, request.Code);
                }

                if (!isValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CODE", Message = "Invalid verification code" }
                    });
                }

                // Generate tokens directly using TokenService
                var tokenService = HttpContext.RequestServices.GetRequiredService<ITokenService>();
                var (accessToken, refreshToken, expiresAt) = await tokenService.GenerateTokensAsync(user, request.RememberMe);

                // Map user to DTO (simplified version)
                var authResponse = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    RequiresEmailVerification = !user.EmailVerified,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        EmailVerified = user.EmailVerified
                    }
                };

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Data = authResponse,
                    Message = "Two-factor authentication verified successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA code");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during verification" }
                });
            }
        }

        /// <summary>
        /// Generates new backup codes (invalidates old ones).
        /// </summary>
        [HttpPost("two-factor/backup-codes")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<string>>>> GenerateBackupCodes()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var isEnabled = await _twoFactorService.IsTwoFactorEnabledAsync(userId);
                if (!isEnabled)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "TWO_FACTOR_NOT_ENABLED", Message = "Two-factor authentication is not enabled" }
                    });
                }

                var backupCodes = await _twoFactorService.GenerateBackupCodesAsync(userId);
                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = backupCodes,
                    Message = "New backup codes generated. Save these codes in a safe place."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating backup codes");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while generating backup codes" }
                });
            }
        }

        /// <summary>
        /// Disables 2FA for the current user.
        /// </summary>
        [HttpPost("two-factor/disable")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DisableTwoFactor()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                await _twoFactorService.DisableTwoFactorAsync(userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Two-factor authentication disabled successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while disabling 2FA" }
                });
            }
        }

        /// <summary>
        /// Gets the 2FA status for the current user.
        /// </summary>
        [HttpGet("two-factor/status")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<TwoFactorStatusResponse>>> GetTwoFactorStatus()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var isEnabled = await _twoFactorService.IsTwoFactorEnabledAsync(userId);
                return Ok(new ApiResponse<TwoFactorStatusResponse>
                {
                    Success = true,
                    Data = new TwoFactorStatusResponse { IsEnabled = isEnabled }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting 2FA status");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while retrieving 2FA status" }
                });
            }
        }

        #endregion

        #region Account Recovery Endpoints

        /// <summary>
        /// Gets available recovery methods for an identifier (email or phone).
        /// </summary>
        [HttpPost("recovery/methods")]
        public async Task<ActionResult<ApiResponse<DTOs.RecoveryMethodsResponse>>> GetRecoveryMethods([FromBody] InitiateRecoveryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Identifier))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Identifier is required" }
                    });
                }

                var methods = await _accountRecoveryService.GetRecoveryMethodsAsync(request.Identifier);
                // Convert service response to DTO
                var dto = new DTOs.RecoveryMethodsResponse
                {
                    HasEmail = methods.HasEmail,
                    HasPhone = methods.HasPhone,
                    HasSecurityQuestions = methods.HasSecurityQuestions,
                    MaskedEmail = methods.MaskedEmail,
                    MaskedPhone = methods.MaskedPhone
                };
                return Ok(new ApiResponse<DTOs.RecoveryMethodsResponse>
                {
                    Success = true,
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recovery methods");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while retrieving recovery methods" }
                });
            }
        }

        /// <summary>
        /// Initiates account recovery via email or phone.
        /// </summary>
        [HttpPost("recovery/initiate")]
        public async Task<ActionResult<ApiResponse<object>>> InitiateRecovery([FromBody] InitiateRecoveryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Method))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Identifier and method are required" }
                    });
                }

                // Rate limiting
                var rateLimitKey = $"recovery:{request.Identifier}";
                var isAllowed = await _rateLimitService.IncrementAndCheckRateLimitAsync(rateLimitKey, 3, TimeSpan.FromHours(1));
                
                if (!isAllowed)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many recovery attempts. Please try again later." }
                    });
                }

                bool success = false;
                if (request.Method.ToLower() == "email")
                {
                    success = await _accountRecoveryService.InitiateEmailRecoveryAsync(request.Identifier);
                }
                else if (request.Method.ToLower() == "phone")
                {
                    success = await _accountRecoveryService.InitiatePhoneRecoveryAsync(request.Identifier);
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_METHOD", Message = "Invalid recovery method" }
                    });
                }

                // Always return success to prevent enumeration
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If an account exists with this identifier, recovery instructions have been sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating recovery");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during recovery" }
                });
            }
        }

        /// <summary>
        /// Verifies a recovery code (from email or SMS).
        /// </summary>
        [HttpPost("recovery/verify-code")]
        public async Task<ActionResult<ApiResponse<object>>> VerifyRecoveryCode([FromBody] VerifyRecoveryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Identifier and code are required" }
                    });
                }

                var method = request.Method.ToLower() switch
                {
                    "email" => Services.RecoveryMethod.Email,
                    "phone" => Services.RecoveryMethod.Phone,
                    _ => throw new ArgumentException("Invalid recovery method")
                };

                var isValid = await _accountRecoveryService.VerifyRecoveryCodeAsync(request.Identifier, request.Code, method);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CODE", Message = "Invalid recovery code" }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Recovery code verified successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying recovery code");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during verification" }
                });
            }
        }

        /// <summary>
        /// Gets available security questions for setup.
        /// </summary>
        [HttpGet("recovery/security-questions")]
        public ActionResult<ApiResponse<List<string>>> GetSecurityQuestions()
        {
            // Return standard security questions
            var questions = new List<string>
            {
                "What was the name of your first pet?",
                "What city were you born in?",
                "What was your mother's maiden name?",
                "What was the name of your elementary school?",
                "What was your childhood nickname?",
                "What was the make of your first car?",
                "What was your favorite food as a child?",
                "What was the name of your best friend growing up?",
                "What street did you grow up on?",
                "What was your favorite teacher's name?"
            };

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = questions,
                Message = "Available security questions retrieved successfully"
            });
        }

        /// <summary>
        /// Sets up security questions for the current user.
        /// </summary>
        [HttpPost("recovery/security-questions")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> SetupSecurityQuestions([FromBody] SetupSecurityQuestionsRequest request)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                if (request.Questions == null || request.Questions.Count == 0 || request.Questions.Count > 3)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Must provide 1-3 security questions" }
                    });
                }

                // Convert DTOs to service requests
                var questions = request.Questions.Select((q, index) => new Services.SecurityQuestionRequest
                {
                    Question = q.Question,
                    Answer = q.Answer,
                    Order = q.Order > 0 ? q.Order : index + 1
                }).ToList();

                await _accountRecoveryService.SetupSecurityQuestionsAsync(userId, questions);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Security questions set up successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up security questions");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while setting up security questions" }
                });
            }
        }

        /// <summary>
        /// Verifies a security question answer.
        /// </summary>
        [HttpPost("recovery/verify-question")]
        public async Task<ActionResult<ApiResponse<object>>> VerifySecurityQuestion([FromBody] VerifySecurityQuestionRequest request, [FromQuery] string identifier)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.Answer))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Identifier, question, and answer are required" }
                    });
                }

                // Find user by email or phone
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == identifier || u.Phone == identifier);

                if (user == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "Invalid identifier or answer" }
                    });
                }

                var isValid = await _accountRecoveryService.VerifySecurityQuestionAsync(user.Id, request.Question, request.Answer);
                
                if (!isValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "Invalid identifier or answer" }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Security question verified successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying security question");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during verification" }
                });
            }
        }

        #endregion

        #region Account Deletion Endpoints

        /// <summary>
        /// Initiates account deletion (soft delete with grace period).
        /// </summary>
        [HttpPost("account/delete")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                if (!request.ConfirmDeletion)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Account deletion must be confirmed" }
                    });
                }

                var success = await _accountDeletionService.InitiateAccountDeletionAsync(userId, request.Password);
                
                if (!success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "Invalid password" }
                    });
                }

                var deletionDate = await _accountDeletionService.GetDeletionDateAsync(userId);
                var daysRemaining = deletionDate.HasValue 
                    ? (int)(deletionDate.Value - DateTime.UtcNow).TotalDays 
                    : 0;

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Account deletion initiated. Your account will be permanently deleted in {daysRemaining} days. You can cancel this action within the grace period."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "BAD_REQUEST", Message = ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating account deletion");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred during account deletion" }
                });
            }
        }

        /// <summary>
        /// Cancels account deletion if within grace period.
        /// </summary>
        [HttpPost("account/cancel-deletion")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> CancelAccountDeletion()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var success = await _accountDeletionService.CancelAccountDeletionAsync(userId);
                
                if (!success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "BAD_REQUEST", Message = "Account deletion cannot be cancelled. Either the account is not pending deletion or the grace period has expired." }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Account deletion cancelled successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "BAD_REQUEST", Message = ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling account deletion");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while cancelling account deletion" }
                });
            }
        }

        /// <summary>
        /// Gets account deletion status.
        /// </summary>
        [HttpGet("account/deletion-status")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AccountDeletionStatusResponse>>> GetAccountDeletionStatus()
        {
            try
            {
                if (!ControllerHelpers.TryGetUserId(User, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "Authentication required" }
                    });
                }

                var isPending = await _accountDeletionService.IsDeletionPendingAsync(userId);
                var deletionDate = await _accountDeletionService.GetDeletionDateAsync(userId);
                var daysRemaining = deletionDate.HasValue 
                    ? (int)(deletionDate.Value - DateTime.UtcNow).TotalDays 
                    : 0;

                return Ok(new ApiResponse<AccountDeletionStatusResponse>
                {
                    Success = true,
                    Data = new AccountDeletionStatusResponse
                    {
                        IsPending = isPending,
                        DeletionDate = deletionDate,
                        DaysRemaining = daysRemaining
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account deletion status");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while retrieving account deletion status" }
                });
            }
        }

        #endregion
    }
}

