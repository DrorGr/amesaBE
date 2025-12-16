using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Contracts;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;
using Npgsql;
using SharedConfigService = AmesaBackend.Shared.Configuration.IConfigurationService;

namespace AmesaBackend.Lottery.Services
{
    public class LotteryService : ILotteryService
    {
        private readonly LotteryDbContext _context;
        private readonly AuthDbContext? _authContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<LotteryService> _logger;
        private readonly IUserPreferencesService? _userPreferencesService;
        private readonly SharedConfigService? _configurationService;
        private readonly IHttpRequest? _httpRequest;
        private readonly IConfiguration? _configuration;
        private readonly IConnectionMultiplexer? _redis;

        public LotteryService(
            LotteryDbContext context, 
            IEventPublisher eventPublisher, 
            ILogger<LotteryService> logger,
            IUserPreferencesService? userPreferencesService = null,
            SharedConfigService? configurationService = null,
            AuthDbContext? authContext = null,
            IHttpRequest? httpRequest = null,
            IConfiguration? configuration = null,
            IConnectionMultiplexer? redis = null)
        {
            _context = context;
            _authContext = authContext;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _userPreferencesService = userPreferencesService;
            _configurationService = configurationService;
            _httpRequest = httpRequest;
            _configuration = configuration;
            _redis = redis;
        }

        /// <summary>
        /// Check if user verification is required and if user is verified
        /// </summary>
        /// <summary>
        /// Checks if user has completed identity verification (required for ticket purchases).
        /// Always enforces verification requirement - identity verification is mandatory for ticket purchases.
        /// </summary>
        public async Task CheckVerificationRequirementAsync(Guid userId)
        {
            // Check user verification status using EF Core (preferred over raw SQL)
            // Use AuthDbContext to query users table with proper type safety
            if (_authContext == null)
            {
                _logger.LogWarning("AuthDbContext not available, cannot verify identity for user {UserId}", userId);
                throw new UnauthorizedAccessException("ID_VERIFICATION_REQUIRED: Identity verification check unavailable");
            }

            var user = await _authContext.Users
                .Where(u => u.Id == userId && u.DeletedAt == null)
                .Select(u => new { u.VerificationStatus })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new UnauthorizedAccessException("ID_VERIFICATION_REQUIRED: User not found");
            }

            // Require IdentityVerified or FullyVerified status
            // Identity verification is mandatory for ticket purchases (Step 4 requirement)
            if (user.VerificationStatus != UserVerificationStatus.IdentityVerified && 
                user.VerificationStatus != UserVerificationStatus.FullyVerified)
            {
                throw new UnauthorizedAccessException("ID_VERIFICATION_REQUIRED: Identity verification required to purchase lottery tickets. Please complete identity verification in your account settings.");
            }
        }

        public async Task<List<LotteryTicketDto>> GetUserTicketsAsync(Guid userId)
        {
            var tickets = await _context.LotteryTickets
                .Include(t => t.House)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            return tickets.Select(MapToTicketDto).ToList();
        }

        public async Task<LotteryTicketDto> GetTicketAsync(Guid ticketId)
        {
            var ticket = await _context.LotteryTickets
                .Include(t => t.House)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException("Ticket not found");
            }

            return MapToTicketDto(ticket);
        }

        public async Task<List<LotteryDrawDto>> GetDrawsAsync()
        {
            var draws = await _context.LotteryDraws
                .Include(d => d.House)
                .OrderByDescending(d => d.DrawDate)
                .ToListAsync();

            return draws.Select(MapToDrawDto).ToList();
        }

        public async Task<LotteryDrawDto> GetDrawAsync(Guid drawId)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
            {
                throw new KeyNotFoundException("Draw not found");
            }

            return MapToDrawDto(draw);
        }

        public async Task<List<ParticipantDto>> GetDrawParticipantsAsync(Guid drawId)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
            {
                throw new KeyNotFoundException($"Draw {drawId} not found");
            }

            // Get all tickets for this draw and group by user
            var participants = await _context.LotteryTickets
                .Where(t => t.HouseId == draw.HouseId && t.Status == "Active")
                .GroupBy(t => t.UserId)
                .Select(g => new ParticipantDto
                {
                    UserId = g.Key,
                    TicketCount = g.Count()
                })
                .ToListAsync();

            return participants;
        }

        public async Task ConductDrawAsync(Guid drawId, ConductDrawRequest request)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
            {
                throw new KeyNotFoundException("Draw not found");
            }

            if (draw.DrawStatus != "Pending")
            {
                throw new InvalidOperationException("Draw has already been conducted");
            }

            // Use transaction to ensure atomicity of draw execution and winner selection
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update draw status and metadata
                draw.DrawStatus = "Completed";
                draw.ConductedAt = DateTime.UtcNow;
                draw.DrawMethod = request.DrawMethod;
                draw.DrawSeed = request.DrawSeed;

                // Select winner if there are active tickets
                var activeTickets = await _context.LotteryTickets
                    .Where(t => t.HouseId == draw.HouseId && t.Status == "Active")
                    .ToListAsync();

                if (activeTickets.Count > 0)
                {
                    // Use DrawSeed for reproducible random selection
                    var random = CreateSeededRandom(request.DrawSeed);
                    var winningTicket = activeTickets[random.Next(activeTickets.Count)];

                    // Mark ticket as winner
                    winningTicket.IsWinner = true;
                    winningTicket.UpdatedAt = DateTime.UtcNow;

                    // Update draw record with winner information
                    draw.WinnerUserId = winningTicket.UserId;
                    draw.WinningTicketId = winningTicket.Id;
                    draw.WinningTicketNumber = winningTicket.TicketNumber;

                    _logger.LogInformation(
                        "Winner selected for draw {DrawId}: Ticket {TicketId} (User {UserId}, TicketNumber {TicketNumber})",
                        draw.Id, winningTicket.Id, winningTicket.UserId, winningTicket.TicketNumber);
                }
                else
                {
                    _logger.LogWarning("No active tickets found for draw {DrawId}, house {HouseId}", draw.Id, draw.HouseId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Publish draw completed event
                await _eventPublisher.PublishAsync(new LotteryDrawCompletedEvent
                {
                    DrawId = draw.Id,
                    HouseId = draw.HouseId,
                    DrawDate = draw.DrawDate,
                    TotalTickets = draw.TotalTicketsSold
                });

                // Publish winner selected event if winner was selected
                if (draw.WinnerUserId.HasValue && draw.WinningTicketId.HasValue)
                {
                    // Parse ticket number (format: {HouseId}-{Number})
                    var ticketNumberParts = draw.WinningTicketNumber?.Split('-');
                    var ticketNumberInt = 0;
                    if (ticketNumberParts != null && ticketNumberParts.Length > 1)
                    {
                        int.TryParse(ticketNumberParts[1], out ticketNumberInt);
                    }

                    // Fetch house information for prize details
                    var house = await _context.Houses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(h => h.Id == draw.HouseId);

                    await _eventPublisher.PublishAsync(new LotteryDrawWinnerSelectedEvent
                    {
                        DrawId = draw.Id,
                        HouseId = draw.HouseId,
                        WinnerTicketId = draw.WinningTicketId.Value,
                        WinnerUserId = draw.WinnerUserId.Value,
                        WinningTicketNumber = ticketNumberInt,
                        HouseTitle = house?.Title,
                        PrizeValue = house?.Price ?? house?.TicketPrice ?? 0,
                        PrizeDescription = house != null ? $"House Prize: {house.Title}" : "House Prize"
                    });
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Creates a seeded random number generator for reproducible winner selection
        /// </summary>
        private Random CreateSeededRandom(string seed)
        {
            // Convert seed string to integer hash for Random seed
            var seedHash = seed.GetHashCode();
            return new Random(seedHash);
        }

        public async Task<List<HouseDto>> GetUserFavoriteHousesAsync(Guid userId)
        {
            List<Guid> favoriteHouseIds;
            
            if (_userPreferencesService != null)
            {
                favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId);
            }
            else
            {
                _logger.LogWarning("UserPreferencesService not available, returning empty favorites list");
                return new List<HouseDto>();
            }

            if (favoriteHouseIds.Count == 0)
            {
                return new List<HouseDto>();
            }

            var houses = await _context.Houses
                .Include(h => h.Images.OrderBy(img => img.DisplayOrder))
                .Where(h => favoriteHouseIds.Contains(h.Id) && h.DeletedAt == null)
                .OrderBy(h => favoriteHouseIds.IndexOf(h.Id))
                .ToListAsync();

            return houses.Select(MapToHouseDto).ToList();
        }

        public async Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId)
        {
            if (_userPreferencesService == null)
            {
                _logger.LogWarning("UserPreferencesService not available");
                return false;
            }

            // Verify house exists (read-only query - use AsNoTracking for performance)
            var house = await _context.Houses
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == houseId);
            if (house == null || house.DeletedAt != null)
            {
                return false;
            }

            return await _userPreferencesService.AddHouseToFavoritesAsync(userId, houseId);
        }

        public async Task<bool> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId)
        {
            if (_userPreferencesService == null)
            {
                _logger.LogWarning("UserPreferencesService not available");
                return false;
            }

            return await _userPreferencesService.RemoveHouseFromFavoritesAsync(userId, houseId);
        }

        public async Task<List<HouseDto>> GetRecommendedHousesAsync(Guid userId, int limit = 10)
        {
            List<Guid> favoriteHouseIds = new();
            List<string> preferredLocations = new();
            decimal? priceMin = null;
            decimal? priceMax = null;

            // Get user preferences if available
            if (_userPreferencesService != null)
            {
                favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId);
                var lotteryPrefs = await _userPreferencesService.GetLotteryPreferencesAsync(userId);
                if (lotteryPrefs != null)
                {
                    preferredLocations = lotteryPrefs.PreferredLocations;
                    priceMin = lotteryPrefs.PriceRangeMin;
                    priceMax = lotteryPrefs.PriceRangeMax;
                }
            }

            var query = _context.Houses
                .Include(h => h.Images.OrderBy(img => img.DisplayOrder))
                .Where(h => h.DeletedAt == null && h.Status == "Active");

            // Filter by preferred locations if available
            if (preferredLocations.Count > 0)
            {
                query = query.Where(h => preferredLocations.Contains(h.Location));
            }

            // Filter by price range if available
            if (priceMin.HasValue)
            {
                query = query.Where(h => h.Price >= priceMin.Value);
            }
            if (priceMax.HasValue)
            {
                query = query.Where(h => h.Price <= priceMax.Value);
            }

            // Exclude already favorite houses
            if (favoriteHouseIds.Count > 0)
            {
                query = query.Where(h => !favoriteHouseIds.Contains(h.Id));
            }

            var houses = await query
                .OrderByDescending(h => h.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return houses.Select(MapToHouseDto).ToList();
        }

        public async Task<List<LotteryTicketDto>> GetUserActiveEntriesAsync(Guid userId)
        {
            var tickets = await _context.LotteryTickets
                .Include(t => t.House)
                .Where(t => t.UserId == userId && t.Status == "Active")
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            return tickets.Select(MapToTicketDto).ToList();
        }

        public async Task<UserLotteryStatsDto> GetUserLotteryStatsAsync(Guid userId)
        {
            var tickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var activeTickets = tickets.Where(t => t.Status == "Active").ToList();
            var winningTickets = tickets.Where(t => t.IsWinner).ToList();
            var totalSpending = tickets.Sum(t => t.PurchasePrice);

            // Get most entered house
            var favoriteHouseId = tickets
                .GroupBy(t => t.HouseId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            // Get most active month
            var mostActiveMonth = tickets
                .GroupBy(t => t.PurchaseDate.ToString("yyyy-MM"))
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var totalEntries = tickets.Count;
            var winRate = totalEntries > 0 ? (decimal)winningTickets.Count / totalEntries : 0;
            var avgSpending = totalEntries > 0 ? totalSpending / totalEntries : 0;

            return new UserLotteryStatsDto
            {
                TotalEntries = totalEntries,
                ActiveEntries = activeTickets.Count,
                TotalWins = winningTickets.Count,
                TotalSpending = totalSpending,
                TotalWinnings = 0, // Will be populated from transactions if available
                WinRate = winRate,
                AverageSpendingPerEntry = avgSpending,
                FavoriteHouseId = favoriteHouseId,
                MostActiveMonth = mostActiveMonth,
                LastEntryDate = tickets.OrderByDescending(t => t.PurchaseDate).FirstOrDefault()?.PurchaseDate
            };
        }

        private HouseDto MapToHouseDto(House house)
        {
            var ticketsSold = house.Tickets?.Count(t => t.Status == "Active") ?? 0;
            var participationPercentage = house.TotalTickets > 0 
                ? (decimal)ticketsSold / house.TotalTickets * 100 
                : 0;
            var canExecute = participationPercentage >= house.MinimumParticipationPercentage;

            // Get unique participants count
            var uniqueParticipants = house.Tickets?
                .Where(t => t.Status == "Active")
                .Select(t => t.UserId)
                .Distinct()
                .Count() ?? 0;

            var isCapReached = house.MaxParticipants.HasValue 
                && uniqueParticipants >= house.MaxParticipants.Value;
            var remainingSlots = house.MaxParticipants.HasValue
                ? Math.Max(0, house.MaxParticipants.Value - uniqueParticipants)
                : (int?)null;

            return new HouseDto
            {
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
                TicketsSold = ticketsSold,
                ParticipationPercentage = participationPercentage,
                CanExecute = canExecute,
                MaxParticipants = house.MaxParticipants,
                UniqueParticipants = uniqueParticipants,
                IsParticipantCapReached = isCapReached,
                RemainingParticipantSlots = remainingSlots,
                ProductId = null, // Will be populated by fetching from Payment service if needed
                Images = house.Images?.Select(img => new HouseImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    AltText = img.AltText,
                    DisplayOrder = img.DisplayOrder,
                    IsPrimary = img.IsPrimary,
                    MediaType = img.MediaType,
                    FileSize = img.FileSize,
                    Width = img.Width,
                    Height = img.Height
                }).ToList() ?? new List<HouseImageDto>(),
                CreatedAt = house.CreatedAt
            };
        }

        private LotteryTicketDto MapToTicketDto(LotteryTicket ticket)
        {
            return new LotteryTicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                HouseId = ticket.HouseId,
                HouseTitle = ticket.House?.Title ?? "",
                PurchasePrice = ticket.PurchasePrice,
                Status = ticket.Status,
                PurchaseDate = ticket.PurchaseDate,
                IsWinner = ticket.IsWinner,
                CreatedAt = ticket.CreatedAt
            };
        }

        private LotteryDrawDto MapToDrawDto(LotteryDraw draw)
        {
            return new LotteryDrawDto
            {
                Id = draw.Id,
                HouseId = draw.HouseId,
                HouseTitle = draw.House?.Title ?? "",
                DrawDate = draw.DrawDate,
                TotalTicketsSold = draw.TotalTicketsSold,
                TotalParticipationPercentage = draw.TotalParticipationPercentage,
                WinningTicketNumber = draw.WinningTicketNumber,
                WinnerUserId = draw.WinnerUserId,
                DrawStatus = draw.DrawStatus,
                DrawMethod = draw.DrawMethod,
                ConductedAt = draw.ConductedAt,
                CreatedAt = draw.CreatedAt
            };
        }

        public async Task<bool> IsParticipantCapReachedAsync(Guid houseId)
        {
            try
            {
                var house = await _context.Houses
                    .AsNoTracking() // Read-only query - use AsNoTracking for performance
                    .FirstOrDefaultAsync(h => h.Id == houseId);
                if (house == null || !house.MaxParticipants.HasValue)
                {
                    return false; // No cap or house not found
                }

                var currentCount = await GetParticipantCountAsync(houseId);
                return currentCount >= house.MaxParticipants.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking participant cap for house {HouseId}", houseId);
                throw;
            }
        }

        public async Task<int> GetParticipantCountAsync(Guid houseId)
        {
            try
            {
                var count = await _context.LotteryTickets
                    .Where(t => t.HouseId == houseId && t.Status == "Active")
                    .Select(t => t.UserId)
                    .Distinct()
                    .CountAsync();

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participant count for house {HouseId}", houseId);
                throw;
            }
        }

        public async Task<bool> CanUserEnterLotteryAsync(Guid userId, Guid houseId, bool useTransaction = true)
        {
            try
            {
                // Check if there's already an active transaction on the context
                var currentTransaction = _context.Database.CurrentTransaction;
                var shouldCreateTransaction = useTransaction && currentTransaction == null;

                if (shouldCreateTransaction)
                {
                    // Create a transaction with Serializable isolation for cap checks to prevent race conditions
                    using var transaction = await _context.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);
                    
                    try
                    {
                        var result = await CheckCanEnterLotteryInternalAsync(userId, houseId);
                        await transaction.CommitAsync();
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {
                    // Use existing transaction (if any) or execute without transaction isolation
                    // If called from within a transaction (e.g., TicketReservationService),
                    // the queries will automatically participate in that transaction
                    return await CheckCanEnterLotteryInternalAsync(userId, houseId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can enter lottery for house {HouseId}", userId, houseId);
                throw;
            }
        }

        /// <summary>
        /// Internal method that performs the actual cap check logic.
        /// Can be called with or without transaction context.
        /// </summary>
        private async Task<bool> CheckCanEnterLotteryInternalAsync(Guid userId, Guid houseId)
        {
            var house = await _context.Houses
                .AsNoTracking() // Read-only query - use AsNoTracking for performance
                .FirstOrDefaultAsync(h => h.Id == houseId);
            if (house == null || !house.MaxParticipants.HasValue)
            {
                return true; // No cap or house not found
            }

            // Check if user already participates (existing participants can always enter)
            var isExistingParticipant = await _context.LotteryTickets
                .AnyAsync(t => t.HouseId == houseId 
                    && t.UserId == userId 
                    && t.Status == "Active");

            if (isExistingParticipant)
            {
                return true; // Existing participant can always enter
            }

            // Check if cap is reached
            var currentCount = await GetParticipantCountAsync(houseId);
            return currentCount < house.MaxParticipants.Value;
        }

        public async Task<LotteryParticipantStatsDto> GetParticipantStatsAsync(Guid houseId)
        {
            try
            {
                // Read-only query for stats calculation - use AsNoTracking for performance
                var house = await _context.Houses
                    .AsNoTracking()
                    .Include(h => h.Tickets)
                    .FirstOrDefaultAsync(h => h.Id == houseId);

                if (house == null)
                {
                    throw new KeyNotFoundException("House not found");
                }

                var activeTickets = house.Tickets?.Where(t => t.Status == "Active").ToList() ?? new List<LotteryTicket>();
                var uniqueParticipants = activeTickets
                    .Select(t => t.UserId)
                    .Distinct()
                    .Count();
                var totalTickets = activeTickets.Count;
                var lastEntryDate = activeTickets
                    .OrderByDescending(t => t.PurchaseDate)
                    .FirstOrDefault()?.PurchaseDate;

                var isCapReached = house.MaxParticipants.HasValue 
                    && uniqueParticipants >= house.MaxParticipants.Value;
                var remainingSlots = house.MaxParticipants.HasValue
                    ? Math.Max(0, house.MaxParticipants.Value - uniqueParticipants)
                    : (int?)null;

                return new LotteryParticipantStatsDto
                {
                    HouseId = house.Id,
                    HouseTitle = house.Title,
                    UniqueParticipants = uniqueParticipants,
                    TotalTickets = totalTickets,
                    MaxParticipants = house.MaxParticipants,
                    IsCapReached = isCapReached,
                    RemainingSlots = remainingSlots,
                    LastEntryDate = lastEntryDate
                };
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participant stats for house {HouseId}", houseId);
                throw;
            }
        }

        /// <summary>
        /// Create lottery tickets directly from payment transaction (called by Payment service)
        /// This method is called after payment is successful and creates tickets immediately
        /// </summary>
        public async Task<CreateTicketsFromPaymentResponse> CreateTicketsFromPaymentAsync(
            CreateTicketsFromPaymentRequest request)
        {
            try
            {
                // Validate house exists (read-only query - use AsNoTracking for performance)
                var house = await _context.Houses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == request.HouseId);
                if (house == null)
                {
                    throw new InvalidOperationException("House not found");
                }

                // Check participant cap with transaction safety
                var canEnter = await CanUserEnterLotteryAsync(request.UserId, request.HouseId, useTransaction: true);
                if (!canEnter)
                {
                    throw new InvalidOperationException("Participant cap reached");
                }

                // Check verification requirement
                await CheckVerificationRequirementAsync(request.UserId);

                // Check idempotency - if tickets already exist for this payment transaction, return existing
                var existingTickets = await _context.LotteryTickets
                    .Where(t => t.PaymentId == request.PaymentId && t.Status == "Active")
                    .ToListAsync();

                if (existingTickets.Any())
                {
                    _logger.LogWarning("Tickets already exist for payment {PaymentId}. Returning existing tickets (idempotency check).", 
                        request.PaymentId);
                    return new CreateTicketsFromPaymentResponse
                    {
                        TicketNumbers = existingTickets.Select(t => t.TicketNumber).ToList(),
                        TicketsPurchased = existingTickets.Count
                    };
                }

                // Create tickets in transaction
                using var transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);
                
                try
                {
                    // Double-check cap within transaction
                    canEnter = await CanUserEnterLotteryAsync(request.UserId, request.HouseId, useTransaction: false);
                    if (!canEnter)
                    {
                        await transaction.RollbackAsync();
                        throw new InvalidOperationException("Participant cap reached");
                    }

                    // Double-check idempotency within transaction (race condition protection)
                    var existingInTransaction = await _context.LotteryTickets
                        .Where(t => t.PaymentId == request.PaymentId && t.Status == "Active")
                        .ToListAsync();

                    if (existingInTransaction.Any())
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning("Tickets already exist for payment {PaymentId} within transaction. Returning existing tickets.", 
                            request.PaymentId);
                        return new CreateTicketsFromPaymentResponse
                        {
                            TicketNumbers = existingInTransaction.Select(t => t.TicketNumber).ToList(),
                            TicketsPurchased = existingInTransaction.Count
                        };
                    }

                    // Generate ticket numbers (atomic operation via Redis or SELECT FOR UPDATE)
                    var baseTicketNumber = await GetNextTicketNumberAsync(request.HouseId, request.Quantity);
                    var tickets = new List<LotteryTicket>();

                    for (int i = 0; i < request.Quantity; i++)
                    {
                        var ticket = new LotteryTicket
                        {
                            Id = Guid.NewGuid(),
                            TicketNumber = $"{request.HouseId.ToString("N")[..8]}-{baseTicketNumber + i:D6}",
                            HouseId = request.HouseId,
                            UserId = request.UserId,
                            PurchasePrice = house.TicketPrice, // Original ticket price
                            PromotionCode = request.PromotionCode, // Store promotion code used
                            DiscountAmount = request.DiscountAmount, // Store discount amount applied
                            Status = "Active",
                            PurchaseDate = DateTime.UtcNow,
                            PaymentId = request.PaymentId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        tickets.Add(ticket);
                    }

                    _context.LotteryTickets.AddRange(tickets);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Created {Count} tickets from payment {PaymentId} for user {UserId}, house {HouseId}",
                        tickets.Count, request.PaymentId, request.UserId, request.HouseId);

                    return new CreateTicketsFromPaymentResponse
                    {
                        TicketNumbers = tickets.Select(t => t.TicketNumber).ToList(),
                        TicketsPurchased = tickets.Count
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tickets from payment {PaymentId}", request.PaymentId);
                throw;
            }
        }

        /// <summary>
        /// Validate ticket purchase before payment processing
        /// Called by Payment service to validate purchase before charging user
        /// </summary>
        public async Task<ValidateTicketsResponse> ValidateTicketsAsync(ValidateTicketsRequest request)
        {
            try
            {
                var errors = new List<string>();

                // Validate house exists (read-only query - use AsNoTracking for performance)
                var house = await _context.Houses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == request.HouseId);
                if (house == null)
                {
                    errors.Add("House not found");
                    return new ValidateTicketsResponse
                    {
                        IsValid = false,
                        Errors = errors
                    };
                }

                // Check participant cap
                var canEnter = await CanUserEnterLotteryAsync(request.UserId, request.HouseId, useTransaction: false);
                if (!canEnter)
                {
                    errors.Add("Participant cap reached");
                }

                // Check verification requirement
                try
                {
                    await CheckVerificationRequirementAsync(request.UserId);
                }
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add(ex.Message);
                }

                // Check inventory
                var ticketsSold = await _context.LotteryTickets
                    .CountAsync(t => t.HouseId == request.HouseId && t.Status == "Active");
                
                var availableTickets = house.TotalTickets - ticketsSold;
                if (availableTickets < request.Quantity)
                {
                    errors.Add($"Insufficient tickets available. Only {availableTickets} tickets remaining.");
                }

                // Check if lottery has ended
                if (house.LotteryEndDate <= DateTime.UtcNow)
                {
                    errors.Add("Lottery has ended");
                }

                var totalCost = house.TicketPrice * request.Quantity;

                return new ValidateTicketsResponse
                {
                    IsValid = errors.Count == 0,
                    Errors = errors,
                    TotalCost = totalCost,
                    CanEnter = canEnter
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tickets for house {HouseId}", request.HouseId);
                return new ValidateTicketsResponse
                {
                    IsValid = false,
                    Errors = new List<string> { "Error validating purchase" }
                };
            }
        }

        /// <summary>
        /// Helper method to get next ticket number for a house
        /// Uses Redis atomic increment (thread-safe) or database fallback with SELECT FOR UPDATE
        /// </summary>
        private async Task<int> GetNextTicketNumberAsync(Guid houseId, int quantity = 1)
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

            // Fallback to database: Use SELECT FOR UPDATE to prevent race conditions
            // This ensures atomicity within the Serializable transaction
            var maxTicket = await _context.LotteryTickets
                .FromSqlRaw(
                    "SELECT * FROM amesa_lottery.lottery_tickets WHERE \"HouseId\" = {0} ORDER BY \"TicketNumber\" DESC LIMIT 1 FOR UPDATE",
                    houseId)
                .FirstOrDefaultAsync();

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

        /// <summary>
        /// Process lottery payment via Payment service
        /// </summary>
        public async Task<PaymentProcessResult> ProcessLotteryPaymentAsync(
            Guid userId,
            Guid houseId,
            int ticketCount,
            Guid paymentMethodId,
            decimal totalCost)
        {
            if (_httpRequest == null || _configuration == null)
            {
                throw new InvalidOperationException("HTTP request service or configuration not available");
            }

            try
            {
                // Check identity verification requirement (mandatory for ticket purchases)
                await CheckVerificationRequirementAsync(userId);

                // Get house for description (read-only query - use AsNoTracking for performance)
                var house = await _context.Houses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == houseId);
                if (house == null)
                {
                    throw new KeyNotFoundException("House not found");
                }

                // Use passed totalCost parameter (already includes discount if applicable)
                // Do NOT recalculate - totalCost is passed from controller with discount applied

                // Get payment service URL
                var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                    ?? Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL")
                    ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

                // Create payment request
                var paymentRequest = new
                {
                    PaymentMethodId = paymentMethodId,
                    Amount = totalCost,
                    Currency = "USD",
                    Description = $"Lottery tickets for {house.Title}",
                    ReferenceId = houseId.ToString(),
                    IdempotencyKey = $"{userId}_{houseId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Type = "lottery_ticket"
                };

                // Get JWT token from current context if available
                var token = string.Empty; // Will be automatically added by HttpRequestService from HttpContext

                // Call Payment service
                // Payment service returns Payment.DTOs.ApiResponse format: { Success, Data, Message, Error, Timestamp }
                var response = await _httpRequest.PostRequest<PaymentApiResponse>(
                    $"{paymentServiceUrl}/payments/process",
                    paymentRequest,
                    token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("Payment failed for user {UserId}, house {HouseId}: {Error}",
                        userId, houseId, response?.Error?.Message ?? "Unknown error");
                    
                    return new PaymentProcessResult
                    {
                        Success = false,
                        ErrorMessage = response?.Error?.Message ?? "Payment processing failed"
                    };
                }

                // Parse transaction ID
                if (string.IsNullOrEmpty(response.Data.TransactionId) ||
                    !Guid.TryParse(response.Data.TransactionId, out var transactionId))
                {
                    _logger.LogError("Invalid TransactionId in payment response: {TransactionId}",
                        response.Data.TransactionId);
                    return new PaymentProcessResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid transaction ID received from payment service"
                    };
                }

                _logger.LogInformation("Payment processed successfully for user {UserId}, house {HouseId}, transaction {TransactionId}",
                    userId, houseId, transactionId);

                return new PaymentProcessResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    ProviderTransactionId = response.Data.ProviderTransactionId
                };
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for user {UserId}, house {HouseId}",
                    userId, houseId);
                return new PaymentProcessResult
                {
                    Success = false,
                    ErrorMessage = $"Payment processing error: {ex.Message}"
                };
            }
        }


        /// <summary>
        /// Payment API response wrapper from Payment service
        /// Matches Payment.DTOs.ApiResponse format
        /// </summary>
        private class PaymentApiResponse
        {
            public bool Success { get; set; }
            public PaymentResponseDto? Data { get; set; }
            public string? Message { get; set; }
            public PaymentErrorResponse? Error { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class PaymentProductApiResponse
        {
            public bool Success { get; set; }
            public ProductDto? Data { get; set; }
            public string? Message { get; set; }
            public PaymentErrorResponse? Error { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class PaymentLinkApiResponse
        {
            public bool Success { get; set; }
            public object? Data { get; set; }
            public string? Message { get; set; }
            public PaymentErrorResponse? Error { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class ProductDto
        {
            public Guid Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string ProductType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public decimal BasePrice { get; set; }
            public string Currency { get; set; } = "USD";
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        /// <summary>
        /// Payment error response from Payment service
        /// Matches Payment.DTOs.ErrorResponse format
        /// </summary>
        private class PaymentErrorResponse
        {
            public string Code { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public object? Details { get; set; }
        }

        /// <summary>
        /// Payment response DTO from Payment service
        /// Matches Payment.DTOs.PaymentResponse format
        /// </summary>
        private class PaymentResponseDto
        {
            public bool Success { get; set; }
            public string? TransactionId { get; set; }
            public string? ProviderTransactionId { get; set; }
            public string? Message { get; set; }
            public string? ErrorCode { get; set; }
        }

        /// <summary>
        /// Creates a product and product link for a house in the Payment service
        /// </summary>
        public async Task<Guid?> CreateProductForHouseAsync(Guid houseId, string houseTitle, decimal ticketPrice, Guid? createdBy)
        {
            if (_httpRequest == null || _configuration == null)
            {
                _logger.LogWarning("HTTP request service or configuration not available, skipping product creation for house {HouseId}", houseId);
                return null;
            }

            try
            {
                // Get payment service URL
                var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                    ?? Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL")
                    ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

                // Create product request
                // Use same format as SQL migration: LOTTERY-{houseIdWithoutDashes}
                var productCode = $"LOTTERY-{houseId:N}".Replace("-", "");
                var createProductRequest = new
                {
                    Code = productCode,
                    Name = $"Lottery Ticket - {houseTitle}",
                    Description = $"Lottery ticket for {houseTitle}",
                    ProductType = "lottery_ticket",
                    BasePrice = ticketPrice,
                    Currency = "USD",
                    IsActive = true
                };

                // Get JWT token from current context if available
                var token = string.Empty; // Will be automatically added by HttpRequestService from HttpContext

                // Call Payment service to create product
                var productResponse = await _httpRequest.PostRequest<PaymentProductApiResponse>(
                    $"{paymentServiceUrl}/products",
                    createProductRequest,
                    token);

                if (productResponse == null || !productResponse.Success || productResponse.Data == null)
                {
                    _logger.LogWarning("Failed to create product for house {HouseId}: {Error}",
                        houseId, productResponse?.Error?.Message ?? "Unknown error");
                    return null;
                }

                var productId = productResponse.Data.Id;

                // Create product link
                var linkRequest = new
                {
                    LinkedEntityType = "house",
                    LinkedEntityId = houseId,
                    LinkMetadata = new Dictionary<string, object>
                    {
                        ["HouseTitle"] = houseTitle,
                        ["TicketPrice"] = ticketPrice
                    }
                };

                var linkResponse = await _httpRequest.PostRequest<PaymentLinkApiResponse>(
                    $"{paymentServiceUrl}/products/{productId}/link",
                    linkRequest,
                    token);

                if (linkResponse == null || !linkResponse.Success)
                {
                    _logger.LogWarning("Failed to create product link for house {HouseId}, product {ProductId}: {Error}",
                        houseId, productId, linkResponse?.Error?.Message ?? "Unknown error");
                    // Product was created but link failed - still return product ID
                }

                _logger.LogInformation("Created product {ProductId} for house {HouseId}", productId, houseId);
                return productId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product for house {HouseId}", houseId);
                return null; // Don't fail house creation if product creation fails
            }
        }

        /// <summary>
        /// Gets the product ID for a house from the Payment service
        /// </summary>
        public async Task<Guid?> GetProductIdForHouseAsync(Guid houseId)
        {
            if (_httpRequest == null || _configuration == null)
            {
                return null;
            }

            try
            {
                // Get payment service URL
                var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                    ?? Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL")
                    ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

                // Get JWT token from current context if available
                var token = string.Empty; // Will be automatically added by HttpRequestService from HttpContext

                // Call Payment service to get product by house ID
                var productResponse = await _httpRequest.GetRequest<PaymentProductApiResponse>(
                    $"{paymentServiceUrl}/products/by-house/{houseId}",
                    token);

                if (productResponse == null || !productResponse.Success || productResponse.Data == null)
                {
                    return null;
                }

                return productResponse.Data.Id;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error getting product ID for house {HouseId}", houseId);
                return null;
            }
        }

        public async Task<Dictionary<Guid, Guid?>> GetProductIdsForHousesAsync(List<Guid> houseIds)
        {
            if (_httpRequest == null || _configuration == null || houseIds == null || houseIds.Count == 0)
            {
                return new Dictionary<Guid, Guid?>();
            }

            var result = new Dictionary<Guid, Guid?>();
            
            // Fetch ProductIds in parallel for all houses
            var tasks = houseIds.Select(async houseId =>
            {
                try
                {
                    var productId = await GetProductIdForHouseAsync(houseId);
                    return new { HouseId = houseId, ProductId = productId };
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error getting product ID for house {HouseId} in batch", houseId);
                    return new { HouseId = houseId, ProductId = (Guid?)null };
                }
            });

            var results = await Task.WhenAll(tasks);
            foreach (var r in results)
            {
                result[r.HouseId] = r.ProductId;
            }

            return result;
        }

        /// <summary>
        /// Gets all user IDs who have favorited a specific house
        /// Uses EF Core to load preferences and parses JSONB in memory
        /// </summary>
        public async Task<List<Guid>> GetHouseFavoriteUserIdsAsync(Guid houseId)
        {
            if (_authContext == null)
            {
                _logger.LogWarning("Auth context not available for getting favorite user IDs");
                return new List<Guid>();
            }

            try
            {
                // Use EF Core to load all user preferences with JSON
                var allPreferences = await _authContext.UserPreferences
                    .Where(up => up.PreferencesJson != null && up.PreferencesJson != string.Empty)
                    .ToListAsync();

                var favoriteUserIds = new List<Guid>();

                // Parse JSON for each user and check if house is in favorites
                foreach (var pref in allPreferences)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(pref.PreferencesJson);
                        if (jsonDoc.RootElement.TryGetProperty("lotteryPreferences", out var lotteryPrefs) &&
                            lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds) &&
                            favoriteIds.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var idElement in favoriteIds.EnumerateArray())
                            {
                                if (idElement.ValueKind == JsonValueKind.String &&
                                    Guid.TryParse(idElement.GetString(), out var favoriteHouseId) &&
                                    favoriteHouseId == houseId)
                                {
                                    favoriteUserIds.Add(pref.UserId);
                                    break; // Found, move to next user
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error parsing preferences JSON for user {UserId}", pref.UserId);
                        // Continue with next user
                    }
                }

                return favoriteUserIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite user IDs for house {HouseId}", houseId);
                return new List<Guid>();
            }
        }

        /// <summary>
        /// Gets all user IDs who are participants (have tickets) for a specific house
        /// </summary>
        public async Task<List<Guid>> GetHouseParticipantUserIdsAsync(Guid houseId)
        {
            try
            {
                // Query distinct user IDs from lottery tickets for this house
                var participantUserIds = await _context.LotteryTickets
                    .Where(t => t.HouseId == houseId && t.Status == "Active")
                    .Select(t => t.UserId)
                    .Distinct()
                    .ToListAsync();

                return participantUserIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participant user IDs for house {HouseId}", houseId);
                return new List<Guid>();
            }
        }
    }
}
