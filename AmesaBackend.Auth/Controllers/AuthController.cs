using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using AmesaBackend.Shared.Helpers;

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
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="captchaService">The CAPTCHA verification service.</param>
        /// <param name="rateLimitService">The rate limiting service.</param>
        /// <param name="logger">The logger instance.</param>
        public AuthController(
            IAuthService authService,
            ICaptchaService captchaService,
            IRateLimitService rateLimitService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _captchaService = captchaService;
            _rateLimitService = rateLimitService;
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
                
                // Increment atomically first, then check to prevent race conditions
                await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromHours(1));
                var currentCount = await _rateLimitService.GetCurrentCountAsync(rateLimitKey);
                
                if (currentCount > 5)
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
                // Rate limiting by email - increment first (atomic), then check
                var rateLimitKey = $"login:{request.Email}";
                await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(15));
                var currentCount = await _rateLimitService.GetCurrentCountAsync(rateLimitKey);
                
                if (currentCount > 5)
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
                var isUserNotFound = ex.Message.Contains("USER_NOT_FOUND");
                
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = isUserNotFound ? "USER_NOT_FOUND" : "AUTHENTICATION_ERROR",
                        Message = ex.Message
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
                // Rate limiting for resend verification - increment first (atomic), then check
                var rateLimitKey = $"resend-verification:{request.Email}";
                await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromHours(1));
                var currentCount = await _rateLimitService.GetCurrentCountAsync(rateLimitKey);
                
                if (currentCount > 3)
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
    }
}

