using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Shared.Events;
using AmesaBackend.Auth.Services;
using SharedConfigService = AmesaBackend.Shared.Configuration.IConfigurationService;

namespace AmesaBackend.Lottery.Services
{
    public class LotteryService : ILotteryService
    {
        private readonly LotteryDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<LotteryService> _logger;
        private readonly IUserPreferencesService? _userPreferencesService;
        private readonly SharedConfigService? _configurationService;

        public LotteryService(
            LotteryDbContext context, 
            IEventPublisher eventPublisher, 
            ILogger<LotteryService> logger,
            IUserPreferencesService? userPreferencesService = null,
            SharedConfigService? configurationService = null)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _userPreferencesService = userPreferencesService;
            _configurationService = configurationService;
        }

        /// <summary>
        /// Check if user verification is required and if user is verified
        /// </summary>
        public async Task CheckVerificationRequirementAsync(Guid userId)
        {
            if (_configurationService == null)
            {
                return; // Configuration service not available, skip check
            }

            var isRequired = await _configurationService.IsFeatureEnabledAsync("id_verification_required");
            if (!isRequired)
            {
                return; // Verification not required
            }

            // Check user verification status from amesa_auth.users table
            var sql = "SELECT verification_status FROM amesa_auth.users WHERE id = {0} AND deleted_at IS NULL LIMIT 1";
            var verificationStatuses = await _context.Database
                .SqlQueryRaw<string>(sql, userId)
                .ToListAsync();

            var verificationStatus = verificationStatuses.FirstOrDefault();
            if (verificationStatus != "IdentityVerified")
            {
                throw new UnauthorizedAccessException("ID_VERIFICATION_REQUIRED: Identity verification required to purchase lottery tickets");
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

            draw.DrawStatus = "Completed";
            draw.ConductedAt = DateTime.UtcNow;
            draw.DrawMethod = request.DrawMethod;
            draw.DrawSeed = request.DrawSeed;

            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new LotteryDrawCompletedEvent
            {
                DrawId = draw.Id,
                HouseId = draw.HouseId,
                DrawDate = draw.DrawDate,
                TotalTickets = draw.TotalTicketsSold
            });

            if (draw.WinnerUserId.HasValue && draw.WinningTicketId.HasValue)
            {
                await _eventPublisher.PublishAsync(new LotteryDrawWinnerSelectedEvent
                {
                    DrawId = draw.Id,
                    HouseId = draw.HouseId,
                    WinnerTicketId = draw.WinningTicketId.Value,
                    WinnerUserId = draw.WinnerUserId.Value,
                    WinningTicketNumber = int.Parse(draw.WinningTicketNumber ?? "0")
                });
            }
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

            // Verify house exists
            var house = await _context.Houses.FindAsync(houseId);
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
                var house = await _context.Houses.FindAsync(houseId);
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

        public async Task<bool> CanUserEnterLotteryAsync(Guid userId, Guid houseId)
        {
            try
            {
                var house = await _context.Houses.FindAsync(houseId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can enter lottery for house {HouseId}", userId, houseId);
                throw;
            }
        }

        public async Task<LotteryParticipantStatsDto> GetParticipantStatsAsync(Guid houseId)
        {
            try
            {
                var house = await _context.Houses
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
    }
}
