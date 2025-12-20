using System.Security.Claims;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmesaBackend.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/auth/identity")]
    public class IdentityVerificationController : ControllerBase
    {
        private readonly IIdentityVerificationService _verificationService;
        private readonly ILogger<IdentityVerificationController> _logger;

        public IdentityVerificationController(
            IIdentityVerificationService verificationService,
            ILogger<IdentityVerificationController> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        /// <summary>
        /// Submit ID and selfie for verification
        /// POST /api/v1/auth/identity/verify
        /// </summary>
        [HttpPost("verify")]
        public async Task<ActionResult<ApiResponse<IdentityVerificationResult>>> VerifyIdentity([FromBody] VerifyIdentityRequest request)
        {
            try
            {
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return new UnauthorizedObjectResult(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _verificationService.VerifyIdentityAsync(userId, request);

                if (result.IsVerified)
                {
                    return Ok(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = true,
                        Data = result,
                        Message = "Identity verified successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Data = result,
                        Message = result.RejectionReason ?? "Verification failed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during identity verification");
                return StatusCode(500, new ApiResponse<IdentityVerificationResult>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Get verification status
        /// GET /api/v1/auth/identity/status
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<ApiResponse<IdentityVerificationStatusDto>>> GetVerificationStatus()
        {
            try
            {
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return new UnauthorizedObjectResult(new ApiResponse<IdentityVerificationStatusDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var status = await _verificationService.GetVerificationStatusAsync(userId);

                return Ok(new ApiResponse<IdentityVerificationStatusDto>
                {
                    Success = true,
                    Data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verification status");
                return StatusCode(500, new ApiResponse<IdentityVerificationStatusDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Retry verification after rejection
        /// POST /api/v1/auth/identity/retry
        /// </summary>
        [HttpPost("retry")]
        public async Task<ActionResult<ApiResponse<IdentityVerificationResult>>> RetryVerification([FromBody] VerifyIdentityRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "VALIDATION_ERROR",
                            Message = "Request body is required"
                        }
                    });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.IdFrontImage))
                {
                    return BadRequest(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "VALIDATION_ERROR",
                            Message = "IdFrontImage is required"
                        }
                    });
                }

                if (string.IsNullOrWhiteSpace(request.SelfieImage))
                {
                    return BadRequest(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Error = new ErrorResponse
                        {
                            Code = "VALIDATION_ERROR",
                            Message = "SelfieImage is required"
                        }
                    });
                }

                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return new UnauthorizedObjectResult(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Error = new ErrorResponse { Code = "UNAUTHORIZED", Message = "User not authenticated" }
                    });
                }

                var result = await _verificationService.RetryVerificationAsync(userId, request);

                if (result.IsVerified)
                {
                    return Ok(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = true,
                        Data = result,
                        Message = "Identity verified successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<IdentityVerificationResult>
                    {
                        Success = false,
                        Data = result,
                        Message = result.RejectionReason ?? "Verification failed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during identity verification retry");
                return StatusCode(500, new ApiResponse<IdentityVerificationResult>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }
    }
}

