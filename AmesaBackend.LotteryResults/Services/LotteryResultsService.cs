using Microsoft.EntityFrameworkCore;
using AmesaBackend.LotteryResults.Data;
using AmesaBackend.LotteryResults.Models;
using AmesaBackend.LotteryResults.Services;
using AmesaBackend.Shared.Events;

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

        public LotteryResultsService(
            LotteryResultsDbContext context,
            IQRCodeService qrCodeService,
            IEventPublisher eventPublisher,
            ILogger<LotteryResultsService> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _eventPublisher = eventPublisher;
            _logger = logger;
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

