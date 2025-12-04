using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using System.Security.Claims;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/houses")]
    public class HousesRecommendationsController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<HousesRecommendationsController> _logger;

        public HousesRecommendationsController(
            ILotteryService lotteryService,
            ILogger<HousesRecommendationsController> logger)
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
        /// Get recommended houses for the user
        /// </summary>
        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<List<RecommendedHouseDto>>>> GetRecommendedHouses([FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<List<RecommendedHouseDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                if (limit < 1 || limit > 50)
                {
                    limit = 10;
                }

                var recommendedHouses = await _lotteryService.GetRecommendedHousesAsync(userId.Value, limit);

                // Convert to RecommendedHouseDto with scores and reasons
                var recommendedDtos = recommendedHouses.Select((house, index) => new RecommendedHouseDto
                {
                    // Copy all HouseDto properties
                    Id = house.Id,
                    Title = house.Title,
                    Description = house.Description,
                    Price = house.Price,
                    Location = house.Location,
                    Address = house.Address,
                    Bedrooms = house.Bedrooms,
                    Bathrooms = house.Bathrooms,
                    SquareFeet = house.SquareFeet,
                    PropertyType = house.PropertyType,
                    YearBuilt = house.YearBuilt,
                    LotSize = house.LotSize,
                    Features = house.Features,
                    Status = house.Status,
                    TotalTickets = house.TotalTickets,
                    TicketPrice = house.TicketPrice,
                    LotteryStartDate = house.LotteryStartDate,
                    LotteryEndDate = house.LotteryEndDate,
                    DrawDate = house.DrawDate,
                    MinimumParticipationPercentage = house.MinimumParticipationPercentage,
                    TicketsSold = house.TicketsSold,
                    ParticipationPercentage = house.ParticipationPercentage,
                    CanExecute = house.CanExecute,
                    Images = house.Images,
                    CreatedAt = house.CreatedAt,
                    // Add recommendation-specific fields
                    RecommendationScore = Math.Round(0.9m - (index * 0.1m), 2),
                    Reason = index == 0 ? "Based on your favorites" : "Similar to your preferences"
                }).ToList();

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<List<RecommendedHouseDto>>
                {
                    Success = true,
                    Data = recommendedDtos,
                    Message = "Recommended houses retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recommended houses");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<List<RecommendedHouseDto>>
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






