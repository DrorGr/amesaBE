using Microsoft.EntityFrameworkCore;
using AmesaBackend.LotteryResults.Data;
using AmesaBackend.LotteryResults.Models;
using AmesaBackend.LotteryResults.Services;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AmesaBackend.LotteryResults.Services
{
    /// <summary>
    /// Service for managing lottery results and automatic result creation from draw events
    /// </summary>
    public class LotteryResultsService : ILotteryResultsService
    {
        private readonly LotteryResultsDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<LotteryResultsService> _logger;
        private readonly IHttpRequest? _httpRequest;
        private readonly IConfiguration? _configuration;

        public LotteryResultsService(
            LotteryResultsDbContext context,
            IQRCodeService qrCodeService,
            IEventPublisher eventPublisher,
            ILogger<LotteryResultsService> logger,
            IHttpRequest? httpRequest = null,
            IConfiguration? configuration = null)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _httpRequest = httpRequest;
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a lottery result from a winner selected event
        /// Called automatically when LotteryDrawWinnerSelectedEvent is published
        /// </summary>
        public async Task<LotteryResult> CreateResultFromWinnerEventAsync(
            Guid drawId,
            Guid houseId,
            Guid winnerTicketId,
            Guid winnerUserId,
            string winningTicketNumber,
            string? houseTitle = null,
            decimal? prizeValue = null,
            string? prizeDescription = null)
        {
            try
            {
                // Check if result already exists (idempotency)
                var existingResult = await _context.LotteryResults
                    .FirstOrDefaultAsync(r => r.DrawId == drawId && r.WinnerUserId == winnerUserId);

                if (existingResult != null)
                {
                    _logger.LogInformation(
                        "Lottery result already exists for draw {DrawId}, winner {WinnerUserId}",
                        drawId, winnerUserId);
                    return existingResult;
                }

                // Prize details - use provided values from event, or fall back to defaults
                decimal finalPrizeValue = prizeValue ?? 0;
                string finalPrizeDescription = prizeDescription ?? (houseTitle != null ? $"House Prize: {houseTitle}" : "House Prize");
                string prizeType = "House";

                // Generate QR code data
                var lotteryResultId = Guid.NewGuid();
                var qrCodeData = await _qrCodeService.GenerateQRCodeDataAsync(
                    lotteryResultId,
                    winningTicketNumber,
                    prizePosition: 1);

                // Generate QR code image URL
                var qrCodeImageUrl = _qrCodeService.GenerateQRCodeImageUrl(qrCodeData);

                // Create lottery result
                var result = new LotteryResult
                {
                    Id = lotteryResultId,
                    LotteryId = houseId,
                    DrawId = drawId,
                    WinnerTicketNumber = winningTicketNumber,
                    WinnerUserId = winnerUserId,
                    PrizePosition = 1,
                    PrizeType = prizeType,
                    PrizeValue = finalPrizeValue,
                    PrizeDescription = finalPrizeDescription,
                    QRCodeData = qrCodeData,
                    QRCodeImageUrl = qrCodeImageUrl,
                    IsVerified = false,
                    IsClaimed = false,
                    ResultDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.LotteryResults.Add(result);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created lottery result {ResultId} for draw {DrawId}, winner {WinnerUserId}",
                    result.Id, drawId, winnerUserId);

                // Publish LotteryResultCreatedEvent for notifications
                await _eventPublisher.PublishAsync(new LotteryResultCreatedEvent
                {
                    ResultId = result.Id,
                    DrawId = drawId,
                    WinnerUserId = winnerUserId,
                    WinnerTicketId = winnerTicketId
                });

                // Gamification integration (award points on win and check achievements)
                if (_httpRequest != null && _configuration != null)
                {
                    try
                    {
                        var lotteryServiceUrl = _configuration["LotteryService:BaseUrl"]
                            ?? Environment.GetEnvironmentVariable("LOTTERY_SERVICE_URL")
                            ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com";

                        // Call Lottery service's gamification endpoint to award points
                        var awardPointsRequest = new
                        {
                            UserId = winnerUserId,
                            Points = 100, // +100 points for winning
                            Reason = "Lottery Win",
                            ReferenceId = result.Id
                        };

                        var token = string.Empty; // Service-to-service auth will be handled by middleware
                        await _httpRequest.PostRequest<object>(
                            $"{lotteryServiceUrl}/api/v1/gamification/award-points",
                            awardPointsRequest,
                            token);

                        _logger.LogInformation("Awarded 100 points to winner {WinnerUserId} for lottery win", winnerUserId);

                        // Check for win-based achievements
                        var checkAchievementsRequest = new
                        {
                            UserId = winnerUserId,
                            ActionType = "Win",
                            ActionData = new
                            {
                                DrawId = drawId,
                                PrizeValue = finalPrizeValue,
                                ResultId = result.Id
                            }
                        };

                        // Use dynamic response since AchievementDto is in a different service
                        var achievementsResponse = await _httpRequest.PostRequest<StandardApiResponse<object>>(
                            $"{lotteryServiceUrl}/api/v1/gamification/check-achievements",
                            checkAchievementsRequest,
                            token);

                        if (achievementsResponse?.Success == true && achievementsResponse.Data != null)
                        {
                            // Try to extract count from response (Data should be a list)
                            if (achievementsResponse.Data is System.Text.Json.JsonElement jsonElement &&
                                jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                var count = jsonElement.GetArrayLength();
                                if (count > 0)
                                {
                                    _logger.LogInformation(
                                        "Unlocked {Count} achievement(s) for winner {WinnerUserId}",
                                        count,
                                        winnerUserId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail result creation if gamification fails
                        _logger.LogWarning(ex, "Failed to update gamification for winner {WinnerUserId} after lottery win", winnerUserId);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating lottery result for draw {DrawId}, winner {WinnerUserId}",
                    drawId, winnerUserId);
                throw;
            }
        }
    }

    public interface ILotteryResultsService
    {
        Task<LotteryResult> CreateResultFromWinnerEventAsync(
            Guid drawId,
            Guid houseId,
            Guid winnerTicketId,
            Guid winnerUserId,
            string winningTicketNumber,
            string? houseTitle = null,
            decimal? prizeValue = null,
            string? prizeDescription = null);
    }
}

