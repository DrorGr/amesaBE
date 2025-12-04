using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Auth.Data;

namespace AmesaBackend.Lottery.Services
{
    public class WatchlistService : IWatchlistService
    {
        private readonly LotteryDbContext _context;
        private readonly AuthDbContext? _authContext;
        private readonly ILogger<WatchlistService> _logger;

        public WatchlistService(
            LotteryDbContext context,
            ILogger<WatchlistService> logger,
            AuthDbContext? authContext = null)
        {
            _context = context;
            _authContext = authContext;
            _logger = logger;
        }

        public async Task<bool> AddToWatchlistAsync(Guid userId, Guid houseId, bool notificationEnabled = true)
        {
            try
            {
                // Validate user exists (cross-schema check)
                var userExists = await ValidateUserExistsAsync(userId);
                if (!userExists)
                {
                    _logger.LogWarning("Attempted to add watchlist item for non-existent user: {UserId}", userId);
                    throw new KeyNotFoundException("User not found");
                }

                // Validate house exists
                var house = await _context.Houses
                    .FirstOrDefaultAsync(h => h.Id == houseId && h.DeletedAt == null);
                
                if (house == null)
                {
                    _logger.LogWarning("Attempted to add non-existent house to watchlist: {HouseId}", houseId);
                    throw new KeyNotFoundException("House not found");
                }

                // Check if already in watchlist
                var exists = await _context.UserWatchlist
                    .AnyAsync(w => w.UserId == userId && w.HouseId == houseId);

                if (exists)
                {
                    _logger.LogInformation("House {HouseId} already in watchlist for user {UserId}", houseId, userId);
                    throw new InvalidOperationException("WATCHLIST_ITEM_EXISTS: House already in watchlist");
                }

                // Add to watchlist
                var watchlistItem = new UserWatchlist
                {
                    UserId = userId,
                    HouseId = houseId,
                    NotificationEnabled = notificationEnabled
                };

                _context.UserWatchlist.Add(watchlistItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added house {HouseId} to watchlist for user {UserId}", houseId, userId);
                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding house {HouseId} to watchlist for user {UserId}", houseId, userId);
                throw;
            }
        }

        public async Task<bool> RemoveFromWatchlistAsync(Guid userId, Guid houseId)
        {
            try
            {
                var watchlistItem = await _context.UserWatchlist
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.HouseId == houseId);

                if (watchlistItem == null)
                {
                    _logger.LogWarning("Attempted to remove non-existent watchlist item: User {UserId}, House {HouseId}", userId, houseId);
                    throw new KeyNotFoundException("WATCHLIST_ITEM_NOT_FOUND: House not in watchlist");
                }

                _context.UserWatchlist.Remove(watchlistItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Removed house {HouseId} from watchlist for user {UserId}", houseId, userId);
                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing house {HouseId} from watchlist for user {UserId}", houseId, userId);
                throw;
            }
        }

        public async Task<List<HouseDto>> GetUserWatchlistAsync(Guid userId)
        {
            try
            {
                var watchlistItems = await _context.UserWatchlist
                    .Include(w => w.House)
                        .ThenInclude(h => h.Images.OrderBy(img => img.DisplayOrder))
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                var houses = watchlistItems.Select(w => w.House).ToList();

                return houses.Select(MapToHouseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watchlist for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsInWatchlistAsync(Guid userId, Guid houseId)
        {
            try
            {
                return await _context.UserWatchlist
                    .AnyAsync(w => w.UserId == userId && w.HouseId == houseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if house {HouseId} is in watchlist for user {UserId}", houseId, userId);
                throw;
            }
        }

        public async Task<bool> ToggleNotificationAsync(Guid userId, Guid houseId, bool enabled)
        {
            try
            {
                var watchlistItem = await _context.UserWatchlist
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.HouseId == houseId);

                if (watchlistItem == null)
                {
                    throw new KeyNotFoundException("WATCHLIST_ITEM_NOT_FOUND: House not in watchlist");
                }

                watchlistItem.NotificationEnabled = enabled;
                watchlistItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Toggled notification for house {HouseId} in watchlist for user {UserId} to {Enabled}", 
                    houseId, userId, enabled);
                return true;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling notification for house {HouseId} in watchlist for user {UserId}", houseId, userId);
                throw;
            }
        }

        public async Task<int> GetWatchlistCountAsync(Guid userId)
        {
            try
            {
                return await _context.UserWatchlist
                    .CountAsync(w => w.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watchlist count for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<WatchlistItemDto>> GetUserWatchlistItemsAsync(Guid userId, int? page = null, int? limit = null)
        {
            try
            {
                IQueryable<Models.UserWatchlist> query = _context.UserWatchlist
                    .Include(w => w.House)
                        .ThenInclude(h => h.Images.OrderBy(img => img.DisplayOrder))
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.CreatedAt);

                // Apply pagination if parameters provided
                if (page.HasValue && limit.HasValue && page.Value > 0 && limit.Value > 0)
                {
                    var validLimit = Math.Min(limit.Value, 100); // Max 100 items per page
                    query = query
                        .Skip((page.Value - 1) * validLimit)
                        .Take(validLimit);
                }

                var watchlistItems = await query.ToListAsync();

                return watchlistItems.Select(w => new WatchlistItemDto
                {
                    Id = w.Id,
                    HouseId = w.HouseId,
                    House = MapToHouseDto(w.House),
                    NotificationEnabled = w.NotificationEnabled,
                    AddedAt = w.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watchlist items for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Validate user exists in amesa_auth.users using EF Core (type-safe, no raw SQL)
        /// </summary>
        private async Task<bool> ValidateUserExistsAsync(Guid userId)
        {
            // Use EF Core with AuthDbContext instead of raw SQL for type safety and security
            if (_authContext == null)
            {
                _logger.LogWarning("AuthDbContext not available, skipping user validation for {UserId}", userId);
                return true; // Fail-open: allow if AuthDbContext not injected
            }

            try
            {
                var userExists = await _authContext.Users
                    .AnyAsync(u => u.Id == userId && u.DeletedAt == null);
                
                return userExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user existence for {UserId}", userId);
                return false;
            }
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
    }
}

