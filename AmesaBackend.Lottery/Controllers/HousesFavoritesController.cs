using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/houses")]
    public class HousesFavoritesController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<HousesFavoritesController> _logger;

        public HousesFavoritesController(
            ILotteryService lotteryService,
            ILogger<HousesFavoritesController> logger)
        {
            _lotteryService = lotteryService;
            _logger = logger;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Get user's favorite houses
        /// </summary>
        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<HouseDto>>>> GetFavoriteHouses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<List<HouseDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var favoriteHouses = await _lotteryService.GetUserFavoriteHousesAsync(userId.Value);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<HouseDto>>
                {
                    Success = true,
                    Data = favoriteHouses,
                    Message = "Favorite houses retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite houses");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<HouseDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Add a house to user's favorites
        /// </summary>
        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>>> AddToFavorites(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var success = await _lotteryService.AddHouseToFavoritesAsync(userId.Value, id);

                if (!success)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "Failed to add house to favorites. House may not exist or already be in favorites."
                    });
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = true,
                    Data = new FavoriteHouseResponse
                    {
                        HouseId = id,
                        Added = true,
                        Message = "House added to favorites successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding house {HouseId} to favorites", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Remove a house from user's favorites
        /// </summary>
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>>> RemoveFromFavorites(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var success = await _lotteryService.RemoveHouseFromFavoritesAsync(userId.Value, id);

                if (!success)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "Failed to remove house from favorites. House may not be in favorites."
                    });
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = true,
                    Data = new FavoriteHouseResponse
                    {
                        HouseId = id,
                        Added = false,
                        Message = "House removed from favorites successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing house {HouseId} from favorites", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }
    }
}






