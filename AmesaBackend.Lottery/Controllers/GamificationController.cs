using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Shared.Contracts;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers
{
    /// <summary>
    /// Controller for gamification endpoints
    /// </summary>
    [ApiController]
    [Route("api/v1/gamification")]
    [Authorize]
    public class GamificationController : ControllerBase
    {
        private readonly IGamificationService _gamificationService;
        private readonly ILogger<GamificationController> _logger;

        public GamificationController(
            IGamificationService gamificationService,
            ILogger<GamificationController> logger)
        {
            _gamificationService = gamificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get user gamification data (points, level, tier, streak, achievements)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<StandardApiResponse<UserGamificationDto>>> GetUserGamification()
        {
            Guid? userId = null;
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var parsedUserId))
                {
                    return Unauthorized(new StandardApiResponse<UserGamificationDto>
                    {
                        Success = false,
                        Error = new StandardErrorResponse
                        {
                            Code = "AUTHENTICATION_ERROR",
                            Message = "User ID not found in token"
                        }
                    });
                }
                userId = parsedUserId;

                var gamification = await _gamificationService.GetUserGamificationAsync(userId.Value);
                
                return Ok(new StandardApiResponse<UserGamificationDto>
                {
                    Success = true,
                    Data = gamification
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error getting user gamification data for user {UserId}", userId);
                return StatusCode(503, new StandardApiResponse<UserGamificationDto>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Gamification service is temporarily unavailable"
                    }
                });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                _logger.LogError(dbEx, "Database connectivity error getting user gamification data for user {UserId}", userId);
                return StatusCode(503, new StandardApiResponse<UserGamificationDto>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Gamification service is temporarily unavailable"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user gamification data for user {UserId}", userId);
                return StatusCode(500, new StandardApiResponse<UserGamificationDto>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving gamification data"
                    }
                });
            }
        }

        /// <summary>
        /// Get user achievements
        /// </summary>
        [HttpGet("achievements")]
        public async Task<ActionResult<StandardApiResponse<List<AchievementDto>>>> GetUserAchievements()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new StandardApiResponse<List<AchievementDto>>
                    {
                        Success = false,
                        Error = new StandardErrorResponse
                        {
                            Code = "AUTHENTICATION_ERROR",
                            Message = "User ID not found in token"
                        }
                    });
                }

                var achievements = await _gamificationService.GetUserAchievementsAsync(userId);
                
                return Ok(new StandardApiResponse<List<AchievementDto>>
                {
                    Success = true,
                    Data = achievements
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user achievements");
                return StatusCode(500, new StandardApiResponse<List<AchievementDto>>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while retrieving achievements"
                    }
                });
            }
        }

        /// <summary>
        /// Award points to a user (internal service-to-service endpoint)
        /// Requires service-to-service authentication
        /// </summary>
        [HttpPost("award-points")]
        [AllowAnonymous] // Will be protected by ServiceToServiceAuthMiddleware
        public async Task<ActionResult<StandardApiResponse<object>>> AwardPoints([FromBody] AwardPointsRequest request)
        {
            try
            {
                await _gamificationService.AwardPointsAsync(
                    request.UserId,
                    request.Points,
                    request.Reason,
                    request.ReferenceId);

                return Ok(new StandardApiResponse<object>
                {
                    Success = true,
                    Data = new { Message = "Points awarded successfully" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding points to user {UserId}", request.UserId);
                return StatusCode(500, new StandardApiResponse<object>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while awarding points"
                    }
                });
            }
        }

        /// <summary>
        /// Check and unlock achievements for a user (internal service-to-service endpoint)
        /// Requires service-to-service authentication
        /// </summary>
        [HttpPost("check-achievements")]
        [AllowAnonymous] // Will be protected by ServiceToServiceAuthMiddleware
        public async Task<ActionResult<StandardApiResponse<List<AchievementDto>>>> CheckAchievements([FromBody] CheckAchievementsRequest request)
        {
            try
            {
                var achievements = await _gamificationService.CheckAchievementsAsync(
                    request.UserId,
                    request.ActionType,
                    request.ActionData);

                return Ok(new StandardApiResponse<List<AchievementDto>>
                {
                    Success = true,
                    Data = achievements
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking achievements for user {UserId}", request.UserId);
                return StatusCode(500, new StandardApiResponse<List<AchievementDto>>
                {
                    Success = false,
                    Error = new StandardErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while checking achievements"
                    }
                });
            }
        }

        /// <summary>
        /// Request DTO for awarding points
        /// </summary>
        public class AwardPointsRequest
        {
            public Guid UserId { get; set; }
            public int Points { get; set; }
            public string Reason { get; set; } = string.Empty;
            public Guid? ReferenceId { get; set; }
        }

        /// <summary>
        /// Request DTO for checking achievements
        /// </summary>
        public class CheckAchievementsRequest
        {
            public Guid UserId { get; set; }
            public string ActionType { get; set; } = string.Empty;
            public object? ActionData { get; set; }
        }
    }
}

