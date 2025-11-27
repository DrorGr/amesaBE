using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace AmesaBackend.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
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

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
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
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
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
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
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
    }
}

