using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Auth.Services;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services
{
    public class TicketReservationService : ITicketReservationService
    {
        private readonly LotteryDbContext _context;
        private readonly IRedisInventoryManager _inventoryManager;
        private readonly ILotteryService _lotteryService;
        private readonly IRateLimitService? _rateLimitService;
        private readonly ILogger<TicketReservationService> _logger;

        public TicketReservationService(
            LotteryDbContext context,
            IRedisInventoryManager inventoryManager,
            ILotteryService lotteryService,
            ILogger<TicketReservationService> logger,
            IRateLimitService? rateLimitService = null)
        {
            _context = context;
            _inventoryManager = inventoryManager;
            _lotteryService = lotteryService;
            _logger = logger;
            _rateLimitService = rateLimitService;
        }

        public async Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request, Guid houseId, Guid userId)
        {
            // Validate house exists and is active (read-only query - use AsNoTracking for performance)
            var house = await _context.Houses
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == houseId);
            if (house == null || house.Status != "Active")
            {
                throw new InvalidOperationException("House not found or not active");
            }

            // Check if lottery has ended
            if (house.LotteryEndDate <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Lottery has ended");
            }

            // Check rate limits
            if (_rateLimitService != null)
            {
                var userKey = $"reservation:user:{userId}";
                var canReserve = await _rateLimitService.CheckRateLimitAsync(userKey, limit: 5, window: TimeSpan.FromHours(1));
                if (!canReserve)
                {
                    throw new InvalidOperationException("Rate limit exceeded. Please try again later.");
                }

                var userHouseKey = $"reservation:user:{userId}:house:{houseId}";
                var canReserveHouse = await _rateLimitService.CheckRateLimitAsync(userHouseKey, limit: 10, window: TimeSpan.FromHours(1));
                if (!canReserveHouse)
                {
                    throw new InvalidOperationException("Rate limit exceeded for this house. Please try again later.");
                }
            }

            // Check verification requirement
            await _lotteryService.CheckVerificationRequirementAsync(userId);

            // Get inventory status
            var inventory = await _inventoryManager.GetInventoryStatusAsync(houseId);
            
            if (inventory.IsSoldOut || inventory.AvailableTickets < request.Quantity)
            {
                throw new InvalidOperationException("Insufficient tickets available");
            }

            // Generate reservation token
            var reservationToken = GenerateReservationToken();

            // Wrap participant cap check and reservation creation in transaction to prevent race conditions
            // This ensures atomicity: cap check + reservation creation happen together
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            
            try
            {
                // Check participant cap WITHIN transaction to prevent race conditions
                // Pass useTransaction: false to use the existing transaction rather than creating a new one
                var canEnter = await _lotteryService.CanUserEnterLotteryAsync(userId, houseId, useTransaction: false);
                if (!canEnter)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("Participant cap reached");
                }

                // Reserve inventory atomically
                var reserved = await _inventoryManager.ReserveInventoryAsync(houseId, request.Quantity, reservationToken);
                if (!reserved)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("Failed to reserve inventory. Please try again.");
                }

                // Add participant
                await _inventoryManager.AddParticipantAsync(houseId, userId);

                // Calculate total price
                var totalPrice = house.TicketPrice * request.Quantity;

                // Create reservation
                var reservation = new TicketReservation
                {
                    Id = Guid.NewGuid(),
                    HouseId = houseId,
                    UserId = userId,
                    Quantity = request.Quantity,
                    TotalPrice = totalPrice,
                    PaymentMethodId = request.PaymentMethodId,
                    Status = "pending",
                    ReservationToken = reservationToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TicketReservations.Add(reservation);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                
                // Increment rate limits after successful reservation (outside transaction)
                if (_rateLimitService != null)
                {
                    var userKey = $"reservation:user:{userId}";
                    await _rateLimitService.IncrementRateLimitAsync(userKey, TimeSpan.FromHours(1));

                    var userHouseKey = $"reservation:user:{userId}:house:{houseId}";
                    await _rateLimitService.IncrementRateLimitAsync(userHouseKey, TimeSpan.FromHours(1));
                }

                _logger.LogInformation("Reservation created: {ReservationId} for house {HouseId}, user {UserId}, quantity {Quantity}",
                    reservation.Id, houseId, userId, request.Quantity);

                return MapToDto(reservation);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ReservationDto?> GetReservationAsync(Guid reservationId, Guid userId)
        {
            var reservation = await _context.TicketReservations
                .Include(r => r.House)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            return reservation != null ? MapToDto(reservation) : null;
        }

        public async Task<bool> CancelReservationAsync(Guid reservationId, Guid userId)
        {
            var reservation = await _context.TicketReservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null)
            {
                return false;
            }

            if (reservation.Status != "pending")
            {
                return false; // Already processed
            }

            // Release inventory
            await _inventoryManager.ReleaseInventoryAsync(reservation.HouseId, reservation.Quantity);

            // Update reservation
            reservation.Status = "cancelled";
            reservation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reservation cancelled: {ReservationId}", reservationId);

            return true;
        }

        public async Task<bool> ValidateReservationAsync(Guid reservationId)
        {
            var reservation = await _context.TicketReservations.FindAsync(reservationId);
            
            if (reservation == null)
            {
                return false;
            }

            if (reservation.Status != "pending")
            {
                return false;
            }

            if (reservation.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public async Task<List<ReservationDto>> GetUserReservationsAsync(Guid userId, string? status = null, int? page = null, int? limit = null)
        {
            var query = _context.TicketReservations
                .Include(r => r.House)
                .Where(r => r.UserId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            // Apply pagination if parameters provided
            if (page.HasValue && limit.HasValue && page.Value > 0 && limit.Value > 0)
            {
                var validLimit = Math.Min(limit.Value, 100); // Max 100 items per page
                query = query
                    .Skip((page.Value - 1) * validLimit)
                    .Take(validLimit);
            }

            var reservations = await query.ToListAsync();

            return reservations.Select(MapToDto).ToList();
        }

        private string GenerateReservationToken()
        {
            var uuid = Guid.NewGuid().ToString("N");
            var random = Random.Shared.Next(100000, 999999);
            return $"{uuid}-{random}";
        }

        private ReservationDto MapToDto(TicketReservation reservation)
        {
            return new ReservationDto
            {
                Id = reservation.Id,
                HouseId = reservation.HouseId,
                UserId = reservation.UserId,
                Quantity = reservation.Quantity,
                TotalPrice = reservation.TotalPrice,
                PaymentMethodId = reservation.PaymentMethodId,
                Status = reservation.Status,
                ReservationToken = reservation.ReservationToken,
                ExpiresAt = reservation.ExpiresAt,
                ProcessedAt = reservation.ProcessedAt,
                PaymentTransactionId = reservation.PaymentTransactionId,
                ErrorMessage = reservation.ErrorMessage,
                CreatedAt = reservation.CreatedAt,
                UpdatedAt = reservation.UpdatedAt
            };
        }
    }
}



