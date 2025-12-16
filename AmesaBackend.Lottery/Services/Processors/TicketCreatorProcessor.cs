using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Services;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services.Processors
{
    public interface ITicketCreatorProcessor
    {
        Task<TicketCreationResult> CreateTicketsAsync(
            Guid reservationId,
            Guid transactionId,
            CancellationToken cancellationToken = default);
    }

    public class TicketCreatorProcessor : ITicketCreatorProcessor
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<TicketCreatorProcessor> _logger;
        private readonly ILotteryService _lotteryService;
        private readonly IRedisInventoryManager _inventoryManager;
        private readonly IConnectionMultiplexer? _redis;

        public TicketCreatorProcessor(
            LotteryDbContext context,
            ILogger<TicketCreatorProcessor> logger,
            ILotteryService lotteryService,
            IRedisInventoryManager inventoryManager,
            IConnectionMultiplexer? redis = null)
        {
            _context = context;
            _logger = logger;
            _lotteryService = lotteryService;
            _inventoryManager = inventoryManager;
            _redis = redis;
        }

        public async Task<TicketCreationResult> CreateTicketsAsync(
            Guid reservationId,
            Guid transactionId,
            CancellationToken cancellationToken = default)
        {
            // Wrap entire operation in Serializable transaction to prevent race conditions
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

            try
            {
                // Load reservation with house
                var reservation = await _context.TicketReservations
                    .Include(r => r.House)
                    .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

                if (reservation == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = "Reservation not found"
                    };
                }

                // Idempotency check: Verify reservation is in processing status
                if (reservation.Status != "processing")
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = $"Reservation status is {reservation.Status}, expected processing"
                    };
                }

                // Validate house exists and is active
                if (reservation.House == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = "House not found for reservation"
                    };
                }

                // Validate house status
                if (reservation.House.Status != "Active" && reservation.House.Status != "Upcoming")
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = $"House status is {reservation.House.Status}, tickets cannot be created"
                    };
                }

                // Validate lottery end date
                if (reservation.House.LotteryEndDate <= DateTime.UtcNow)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = "Lottery has ended, tickets cannot be created"
                    };
                }

                // Check user verification (required for ticket purchases)
                try
                {
                    await _lotteryService.CheckVerificationRequirementAsync(reservation.UserId);
                }
                catch (UnauthorizedAccessException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }

                // Check participant cap (within transaction to prevent race conditions)
                var canEnter = await _lotteryService.CanUserEnterLotteryAsync(
                    reservation.UserId, 
                    reservation.HouseId, 
                    useTransaction: false); // Already in transaction

                if (!canEnter)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = "Maximum number of participants reached for this lottery"
                    };
                }

                // Generate ticket numbers using atomic Redis increment (or fallback to database)
                var tickets = new List<LotteryTicket>();
                var baseTicketNumber = await GetNextTicketNumberAsync(reservation.HouseId, reservation.Quantity, cancellationToken);

                for (int i = 0; i < reservation.Quantity; i++)
                {
                    var ticket = new LotteryTicket
                    {
                        Id = Guid.NewGuid(),
                        TicketNumber = $"{reservation.HouseId.ToString("N")[..8]}-{baseTicketNumber + i:D6}",
                        HouseId = reservation.HouseId,
                        UserId = reservation.UserId,
                        PurchasePrice = reservation.House.TicketPrice, // Original ticket price
                        PromotionCode = reservation.PromotionCode, // Store promotion code used
                        DiscountAmount = reservation.DiscountAmount, // Store discount amount
                        Status = "Active",
                        PurchaseDate = DateTime.UtcNow,
                        PaymentId = transactionId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    tickets.Add(ticket);
                }

                // Add tickets to context
                _context.LotteryTickets.AddRange(tickets);

                // Update reservation status
                // Note: PaymentTransactionId is already set in ReservationProcessor before ticket creation
                // We only update status and timestamps here to avoid redundant database writes
                reservation.Status = "completed";
                reservation.ProcessedAt = DateTime.UtcNow;
                reservation.UpdatedAt = DateTime.UtcNow;

                // Save all changes within transaction
                await _context.SaveChangesAsync(cancellationToken);

                // Add participant to Redis BEFORE committing transaction
                // This ensures consistency: if Redis fails, transaction can still rollback
                try
                {
                    await _inventoryManager.AddParticipantAsync(reservation.HouseId, reservation.UserId);
                }
                catch (Exception redisEx)
                {
                    // If Redis fails, rollback transaction to maintain consistency
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(redisEx, 
                        "Failed to add participant to Redis for reservation {ReservationId}, rolling back transaction",
                        reservationId);
                    return new TicketCreationResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to update inventory. Please try again."
                    };
                }

                // Commit transaction only after Redis operation succeeds
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Created {Count} tickets for reservation {ReservationId}, transaction {TransactionId}",
                    tickets.Count, reservationId, transactionId);

                return new TicketCreationResult
                {
                    Success = true,
                    TicketIds = tickets.Select(t => t.Id).ToList()
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating tickets for reservation {ReservationId}", reservationId);
                return new TicketCreationResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create tickets: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets next ticket number using Redis atomic increment (thread-safe) or database fallback
        /// </summary>
        private async Task<int> GetNextTicketNumberAsync(Guid houseId, int quantity, CancellationToken cancellationToken)
        {
            // Try Redis first for atomic increment
            if (_redis != null)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var key = $"lottery:ticket_number:{houseId}";
                    
                    // Atomically increment by quantity and get the starting number
                    var result = await db.StringIncrementAsync(key, quantity);
                    var startingNumber = (int)result - quantity + 1;
                    
                    // Ensure minimum value is 1
                    if (startingNumber < 1)
                    {
                        // Reset if somehow negative
                        await db.StringSetAsync(key, quantity);
                        startingNumber = 1;
                    }
                    
                    return startingNumber;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis ticket number generation failed for house {HouseId}, falling back to database", houseId);
                    // Fall through to database method
                }
            }

            // Fallback to database: Get max ticket number within transaction
            // Note: This is less ideal but works within the Serializable transaction
            var maxTicket = await _context.LotteryTickets
                .Where(t => t.HouseId == houseId)
                .OrderByDescending(t => t.TicketNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (maxTicket == null)
            {
                return 1;
            }

            var parts = maxTicket.TicketNumber.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var number))
            {
                return number + 1;
            }

            return 1;
        }
    }

    public class TicketCreationResult
    {
        public bool Success { get; set; }
        public List<Guid> TicketIds { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}












