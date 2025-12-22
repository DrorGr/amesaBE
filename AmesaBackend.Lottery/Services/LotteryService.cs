using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Lottery.Hubs;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Contracts;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<LotteryHub>? _hubContext;
        private readonly ICache? _cache;
        private readonly IGamificationService? _gamificationService;

        public LotteryService(
            LotteryDbContext context, 
            IEventPublisher eventPublisher, 
            ILogger<LotteryService> logger,
            IUserPreferencesService? userPreferencesService = null,
            SharedConfigService? configurationService = null,
            AuthDbContext? authContext = null,
            IHttpRequest? httpRequest = null,
            IConfiguration? configuration = null,
            IConnectionMultiplexer? redis = null,
            IHubContext<LotteryHub>? hubContext = null,
            ICache? cache = null,
            IGamificationService? gamificationService = null)
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
            _hubContext = hubContext;
            _cache = cache;
            _gamificationService = gamificationService;
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

        public async Task<List<HouseDto>> GetUserFavoriteHousesAsync(Guid userId, int page = 1, int limit = 20, string? sortBy = null, string? sortOrder = null, CancellationToken cancellationToken = default)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (limit < 1) limit = 20;
            if (limit > 100) limit = 100;

            // Validate sorting parameters
            if (string.IsNullOrWhiteSpace(sortBy)) sortBy = "dateAdded";
            if (string.IsNullOrWhiteSpace(sortOrder)) sortOrder = "asc";
            sortBy = sortBy.ToLowerInvariant();
            sortOrder = sortOrder.ToLowerInvariant();

            // Validate sortBy values
            var validSortByValues = new[] { "dateadded", "price", "location", "title" };
            if (!validSortByValues.Contains(sortBy))
            {
                _logger.LogWarning("Invalid sortBy value '{SortBy}' for user {UserId}, defaulting to 'dateadded'", sortBy, userId);
                sortBy = "dateadded";
            }

            // Validate sortOrder values
            if (sortOrder != "asc" && sortOrder != "desc")
            {
                _logger.LogWarning("Invalid sortOrder value '{SortOrder}' for user {UserId}, defaulting to 'asc'", sortOrder, userId);
                sortOrder = "asc";
            }

            List<Guid> favoriteHouseIds;
            
            if (_userPreferencesService != null)
            {
                // Check cache for favorite IDs first
                var favoriteIdsCacheKey = $"lottery:favorites:{userId}";
                try
                {
                    if (_cache != null)
                    {
                        try
                        {
                            var cachedIds = await _cache.GetRecordAsync<List<Guid>>(favoriteIdsCacheKey);
                            if (cachedIds != null)
                            {
                                favoriteHouseIds = cachedIds;
                                _logger.LogDebug("Cache hit for favorite house IDs for user {UserId}", userId);
                            }
                            else
                            {
                                favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId, cancellationToken);
                                // Cache the IDs with 5-minute TTL
                                if (favoriteHouseIds != null)
                                {
                                    await _cache.SetRecordAsync(favoriteIdsCacheKey, favoriteHouseIds, TimeSpan.FromMinutes(5));
                                }
                            }
                        }
                        catch (Exception cacheEx)
                        {
                            _logger.LogWarning(cacheEx, "Failed to get favorite house IDs from cache for user {UserId}, falling back to service", userId);
                            favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId, cancellationToken);
                        }
                    }
                    else
                    {
                        favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId, cancellationToken);
                    }

                    // Ensure favoriteHouseIds is not null
                    if (favoriteHouseIds == null)
                    {
                        _logger.LogWarning("GetFavoriteHouseIdsAsync returned null for user {UserId}, returning empty list", userId);
                        return new List<HouseDto>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting favorite house IDs for user {UserId}, returning empty list", userId);
                    return new List<HouseDto>();
                }
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

            // Check cache for houses list (only for default sort/pagination to avoid cache key complexity)
            List<HouseDto>? cachedHouses = null;
            
            if (_cache != null && page == 1 && limit == 20 && sortBy == "dateadded" && sortOrder == "asc")
            {
                // Only cache the first page with default sorting to avoid cache key explosion
                // Create cache key only when it will be used
                var favoriteHousesCacheKey = $"lottery:favorites:houses:{userId}:{sortBy}:{sortOrder}";
                try
                {
                    cachedHouses = await _cache.GetRecordAsync<List<HouseDto>>(favoriteHousesCacheKey);
                    if (cachedHouses != null)
                    {
                        _logger.LogDebug("Cache hit for favorite houses list for user {UserId}", userId);
                        return cachedHouses;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get favorite houses from cache for user {UserId}, falling back to database", userId);
                }
            }

            // Get all houses first (before pagination) for sorting
            var allHouses = await _context.Houses
                .Include(h => h.Images.OrderBy(img => img.DisplayOrder))
                .Where(h => favoriteHouseIds.Contains(h.Id) && h.DeletedAt == null)
                .ToListAsync(cancellationToken);

            // Filter out null houses and map to DTOs
            var houseDtos = allHouses
                .Where(h => h != null)
                .Select(MapToHouseDto)
                .Where(dto => dto != null)
                .ToList();

            // Apply sorting
            if (sortBy == "dateadded")
            {
                // Sort by DateAdded from JSONB with fallback to CreatedAt or array order
                // Note: DateAdded may not be available for all favorites, so we use fallback
                var dateAddedMap = new Dictionary<Guid, DateTime>();
                
                // Try to extract DateAdded from user preferences JSONB
                if (_userPreferencesService != null)
                {
                    try
                    {
                        var preferences = await _userPreferencesService.GetUserPreferencesAsync(userId, cancellationToken);
                        if (preferences != null && !string.IsNullOrEmpty(preferences.PreferencesJson))
                        {
                            try
                            {
                                var jsonDoc = System.Text.Json.JsonDocument.Parse(preferences.PreferencesJson);
                                if (jsonDoc.RootElement.TryGetProperty("lotteryPreferences", out var lotteryPrefs))
                                {
                                    if (lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds))
                                    {
                                        if (favoriteIds.ValueKind == System.Text.Json.JsonValueKind.Array)
                                        {
                                            foreach (var idElement in favoriteIds.EnumerateArray())
                                            {
                                                Guid? houseId = null;
                                                DateTime? dateAdded = null;
                                                
                                                if (idElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                                                {
                                                    // Object format: { "houseId": "...", "dateAdded": "..." }
                                                    if (idElement.TryGetProperty("houseId", out var houseIdProp) && 
                                                        houseIdProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                                    {
                                                        if (Guid.TryParse(houseIdProp.GetString(), out var guid))
                                                        {
                                                            houseId = guid;
                                                        }
                                                    }
                                                    
                                                    if (idElement.TryGetProperty("dateAdded", out var dateAddedProp) &&
                                                        dateAddedProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                                    {
                                                        if (DateTime.TryParse(dateAddedProp.GetString(), out var parsedDate))
                                                        {
                                                            dateAdded = parsedDate;
                                                        }
                                                    }
                                                }
                                                else if (idElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                                {
                                                    // String format: just the GUID (no DateAdded available)
                                                    if (Guid.TryParse(idElement.GetString(), out var guid))
                                                    {
                                                        houseId = guid;
                                                    }
                                                }
                                                
                                                if (houseId.HasValue && dateAdded.HasValue)
                                                {
                                                    dateAddedMap[houseId.Value] = dateAdded.Value;
                                                }
                                            }
                                        }
                                    }
                                }
                                jsonDoc.Dispose();
                            }
                            catch (Exception jsonEx)
                            {
                                _logger.LogWarning(jsonEx, "Failed to parse DateAdded from preferences JSON for sorting, using fallback");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get DateAdded for sorting, using fallback");
                    }
                }
                
                // Sort by DateAdded with fallback to CreatedAt, then by favoriteHouseIds order
                var idOrder = favoriteHouseIds.Select((id, index) => new { Id = id, Index = index })
                    .ToDictionary(x => x.Id, x => x.Index);
                
                houseDtos = sortOrder == "desc"
                    ? houseDtos.OrderByDescending(h => dateAddedMap.GetValueOrDefault(h.Id, h.CreatedAt))
                        .ThenByDescending(h => idOrder.GetValueOrDefault(h.Id, int.MaxValue)).ToList()
                    : houseDtos.OrderBy(h => dateAddedMap.GetValueOrDefault(h.Id, h.CreatedAt))
                        .ThenBy(h => idOrder.GetValueOrDefault(h.Id, int.MaxValue)).ToList();
            }
            else if (sortBy == "price")
            {
                houseDtos = sortOrder == "desc" 
                    ? houseDtos.OrderByDescending(h => h.Price).ToList()
                    : houseDtos.OrderBy(h => h.Price).ToList();
            }
            else if (sortBy == "location")
            {
                houseDtos = sortOrder == "desc"
                    ? houseDtos.OrderByDescending(h => h.Location).ToList()
                    : houseDtos.OrderBy(h => h.Location).ToList();
            }
            else if (sortBy == "title")
            {
                houseDtos = sortOrder == "desc"
                    ? houseDtos.OrderByDescending(h => h.Title).ToList()
                    : houseDtos.OrderBy(h => h.Title).ToList();
            }
            // Default to dateAdded if invalid sortBy
            else
            {
                var idOrder = favoriteHouseIds.Select((id, index) => new { Id = id, Index = index })
                    .ToDictionary(x => x.Id, x => x.Index);
                houseDtos = houseDtos.OrderBy(h => idOrder.GetValueOrDefault(h.Id, int.MaxValue)).ToList();
            }

            // Apply pagination after sorting
            var skip = (page - 1) * limit;
            var paginatedDtos = houseDtos.Skip(skip).Take(limit).ToList();

            // Cache the full sorted list (first page only) for default sort to improve performance
            // Note: We cache the full sorted list (houseDtos) rather than paginated result to enable
            // fast retrieval of subsequent pages from cache. The cache key is only created when needed.
            if (_cache != null && page == 1 && limit == 20 && sortBy == "dateadded" && sortOrder == "asc" && paginatedDtos.Count > 0)
            {
                try
                {
                    // Create cache key only when caching (matches the key used for retrieval above)
                    var favoriteHousesCacheKey = $"lottery:favorites:houses:{userId}:{sortBy}:{sortOrder}";
                    // Cache the full sorted list (not just paginated) for future use
                    await _cache.SetRecordAsync(favoriteHousesCacheKey, houseDtos, TimeSpan.FromMinutes(5));
                    _logger.LogDebug("Cached favorite houses list for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache favorite houses list for user {UserId}", userId);
                    // Don't fail the operation if caching fails
                }
            }

            return paginatedDtos;
        }

        public async Task<int> GetUserFavoriteHousesCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Check cache first for fast path
            var favoriteIdsCacheKey = $"lottery:favorites:{userId}";
            if (_cache != null)
            {
                try
                {
                    var cachedIds = await _cache.GetRecordAsync<List<Guid>>(favoriteIdsCacheKey);
                    if (cachedIds != null && cachedIds.Count > 0)
                    {
                        // Filter out deleted houses from cached IDs to match GetUserFavoriteHousesAsync behavior
                        var validHouseIds = await _context.Houses
                            .AsNoTracking()
                            .Where(h => cachedIds.Contains(h.Id) && h.DeletedAt == null)
                            .Select(h => h.Id)
                            .ToListAsync(cancellationToken);
                        
                        _logger.LogDebug("Cache hit for favorite house IDs count for user {UserId} (filtered deleted: {Count})", userId, validHouseIds.Count);
                        return validHouseIds.Count;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get favorite house IDs count from cache for user {UserId}, falling back to database", userId);
                }
            }

            // Fallback to database - filter out deleted houses to match GetUserFavoriteHousesAsync behavior
            if (_userPreferencesService != null)
            {
                var favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId, cancellationToken);
                
                // Filter out deleted houses to ensure count matches actual results
                if (favoriteHouseIds.Count > 0)
                {
                    var validHouseIds = await _context.Houses
                        .AsNoTracking()
                        .Where(h => favoriteHouseIds.Contains(h.Id) && h.DeletedAt == null)
                        .Select(h => h.Id)
                        .ToListAsync(cancellationToken);
                    
                    return validHouseIds.Count;
                }
                
                return 0;
            }

            _logger.LogWarning("UserPreferencesService not available, returning 0 for favorites count");
            return 0;
        }

        public async Task<DTOs.FavoriteOperationResult> AddHouseToFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default)
        {
            if (_userPreferencesService == null)
            {
                _logger.LogWarning("UserPreferencesService not available");
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.ServiceUnavailable());
            }

            // Verify house exists (read-only query - use AsNoTracking for performance)
            var house = await _context.Houses
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == houseId, cancellationToken);
            if (house == null)
            {
                _logger.LogWarning("House {HouseId} does not exist, cannot add to favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.HouseNotFound());
            }
            if (house.DeletedAt != null)
            {
                _logger.LogWarning("House {HouseId} is soft-deleted, cannot add to favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.HouseNotFound());
            }

            // Check if already in favorites (direct check - no need to fetch all houses)
            var existingFavorites = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId, cancellationToken);
            if (existingFavorites.Contains(houseId))
            {
                _logger.LogDebug("House {HouseId} is already in favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.AlreadyFavorite());
            }

            var success = await _userPreferencesService.AddHouseToFavoritesAsync(userId, houseId, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Failed to add house {HouseId} to favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.ServiceUnavailable());
            }
            
            // Invalidate cache on successful add
            if (_cache != null)
            {
                const int maxRetries = 3;
                var retryCount = 0;
                var cacheInvalidated = false;
                
                while (retryCount < maxRetries && !cacheInvalidated && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var favoriteIdsCacheKey = $"lottery:favorites:{userId}";
                        // Use Redis pattern matching to invalidate all cache keys for this user's favorite houses (including all sort variations)
                        // Redis uses glob-style patterns: * matches any characters
                        var cachePattern = $"lottery:favorites:houses:{userId}:*";
                        await _cache.RemoveRecordAsync(favoriteIdsCacheKey);
                        await _cache.DeleteByRegex(cachePattern);
                        cacheInvalidated = true;
                        _logger.LogDebug("Invalidated cache for user {UserId} after adding favorite {HouseId} (attempt {Attempt})", userId, houseId, retryCount + 1);
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries || cancellationToken.IsCancellationRequested)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogInformation("Cache invalidation cancelled for user {UserId} after adding favorite {HouseId}", userId, houseId);
                            }
                            else
                            {
                                _logger.LogError(ex, "Failed to invalidate cache for user {UserId} after adding favorite {HouseId} after {Retries} attempts", userId, houseId, maxRetries);
                            }
                            break; // Exit retry loop
                        }
                        else
                        {
                            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId} after adding favorite {HouseId}, retrying ({Attempt}/{MaxRetries})", userId, houseId, retryCount, maxRetries);
                            await Task.Delay(100 * retryCount, cancellationToken); // Exponential backoff
                        }
                    }
                }
            }
            
            // NOTE: Gamification points are awarded by UserPreferencesService.AddHouseToFavoritesAsync
            // No need to award points here to avoid duplicate awards
            
            // Broadcast SignalR update if operation was successful
            if (_hubContext != null)
            {
                try
                {
                    var update = new FavoriteUpdateDto
                    {
                        HouseId = houseId,
                        UpdateType = "added",
                        HouseTitle = house.Title,
                        Timestamp = DateTime.UtcNow
                    };
                    await _hubContext.BroadcastFavoriteUpdate(userId, update, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast favorite update for house {HouseId} to user {UserId}", houseId, userId);
                    // Don't fail the operation if SignalR broadcast fails
                }
            }

            return DTOs.FavoriteOperationResult.CreateSuccess();
        }

        public async Task<DTOs.FavoriteOperationResult> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default)
        {
            if (_userPreferencesService == null)
            {
                _logger.LogWarning("UserPreferencesService not available");
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.ServiceUnavailable());
            }

            // Check if in favorites first (direct check - no need to fetch house if not in favorites)
            var existingFavorites = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId, cancellationToken);
            if (!existingFavorites.Contains(houseId))
            {
                _logger.LogDebug("House {HouseId} is not in favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.NotInFavorites());
            }

            // Verify house exists (read-only query - use AsNoTracking for performance)
            var house = await _context.Houses
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == houseId, cancellationToken);
            if (house == null)
            {
                _logger.LogWarning("House {HouseId} does not exist, cannot remove from favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.HouseNotFound());
            }
            if (house.DeletedAt != null)
            {
                _logger.LogWarning("House {HouseId} is soft-deleted, cannot remove from favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.HouseNotFound());
            }

            var success = await _userPreferencesService.RemoveHouseFromFavoritesAsync(userId, houseId, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Failed to remove house {HouseId} from favorites for user {UserId}", houseId, userId);
                return DTOs.FavoriteOperationResult.CreateError(DTOs.FavoriteOperationError.ServiceUnavailable());
            }
            
            // Invalidate cache on successful remove
            if (_cache != null)
            {
                const int maxRetries = 3;
                var retryCount = 0;
                var cacheInvalidated = false;
                
                while (retryCount < maxRetries && !cacheInvalidated && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var favoriteIdsCacheKey = $"lottery:favorites:{userId}";
                        // Use Redis pattern matching to invalidate all cache keys for this user's favorite houses (including all sort variations)
                        // Redis uses glob-style patterns: * matches any characters
                        var cachePattern = $"lottery:favorites:houses:{userId}:*";
                        await _cache.RemoveRecordAsync(favoriteIdsCacheKey);
                        await _cache.DeleteByRegex(cachePattern);
                        cacheInvalidated = true;
                        _logger.LogDebug("Invalidated cache for user {UserId} after removing favorite {HouseId} (attempt {Attempt})", userId, houseId, retryCount + 1);
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries || cancellationToken.IsCancellationRequested)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogInformation("Cache invalidation cancelled for user {UserId} after removing favorite {HouseId}", userId, houseId);
                            }
                            else
                            {
                                _logger.LogError(ex, "Failed to invalidate cache for user {UserId} after removing favorite {HouseId} after {Retries} attempts", userId, houseId, maxRetries);
                            }
                            break; // Exit retry loop
                        }
                        else
                        {
                            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId} after removing favorite {HouseId}, retrying ({Attempt}/{MaxRetries})", userId, houseId, retryCount, maxRetries);
                            await Task.Delay(100 * retryCount, cancellationToken); // Exponential backoff
                        }
                    }
                }
            }
            
            // Broadcast SignalR update if operation was successful
            if (_hubContext != null)
            {
                try
                {
                    // Reuse house title from validation query above
                    string? houseTitle = house?.Title;

                    var update = new FavoriteUpdateDto
                    {
                        HouseId = houseId,
                        UpdateType = "removed",
                        HouseTitle = houseTitle,
                        Timestamp = DateTime.UtcNow
                    };
                    await _hubContext.BroadcastFavoriteUpdate(userId, update, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast favorite update for house {HouseId} to user {UserId}", houseId, userId);
                    // Don't fail the operation if SignalR broadcast fails
                }
            }

            return DTOs.FavoriteOperationResult.CreateSuccess();
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
                .Where(t => t.UserId == userId && t.Status != null && t.Status.ToLower() == "active")
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            return tickets.Select(MapToTicketDto).ToList();
        }

        public async Task<UserLotteryStatsDto> GetUserLotteryStatsAsync(Guid userId)
        {
            var tickets = await _context.LotteryTickets
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var activeTickets = tickets.Where(t => t.Status != null && t.Status.ToLower() == "active").ToList();
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

            // Calculate TotalWinnings from payment service
            decimal totalWinnings = 0;
            try
            {
                if (_httpRequest != null && _configuration != null)
                {
                    var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                        ?? Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL")
                        ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

                    var token = string.Empty; // Will be automatically added by HttpRequestService from HttpContext

                    // Call payment service to get transactions
                    var transactionsResponse = await _httpRequest.GetRequest<TransactionsApiResponse>(
                        $"{paymentServiceUrl}/payment/transactions",
                        token);

                    if (transactionsResponse != null && transactionsResponse.Success && transactionsResponse.Data != null)
                    {
                        // Filter for winning transactions: Type="Payout" or "Winning", Status="Completed"
                        var winningTransactions = transactionsResponse.Data
                            .Where(t => (t.Type.Equals("Payout", StringComparison.OrdinalIgnoreCase) || 
                                        t.Type.Equals("Winning", StringComparison.OrdinalIgnoreCase)) &&
                                       t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        totalWinnings = winningTransactions.Sum(t => t.Amount);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log warning but don't fail the request - return 0 for TotalWinnings
                _logger.LogWarning(ex, "Failed to calculate TotalWinnings from payment service for user {UserId}. Returning 0.", userId);
            }

            // Get gamification data if available
            UserGamificationDto? gamification = null;
            if (_gamificationService != null)
            {
                try
                {
                    gamification = await _gamificationService.GetUserGamificationAsync(userId);
                }
                catch (Exception ex)
                {
                    // Log warning but don't fail the request - gamification is optional
                    _logger.LogWarning(ex, "Failed to get gamification data for user {UserId}. Continuing without gamification data.", userId);
                }
            }

            return new UserLotteryStatsDto
            {
                TotalEntries = totalEntries,
                ActiveEntries = activeTickets.Count,
                TotalWins = winningTickets.Count,
                TotalSpending = totalSpending,
                TotalWinnings = totalWinnings,
                WinRate = winRate,
                AverageSpendingPerEntry = avgSpending,
                FavoriteHouseId = favoriteHouseId,
                MostActiveMonth = mostActiveMonth,
                LastEntryDate = tickets.OrderByDescending(t => t.PurchaseDate).FirstOrDefault()?.PurchaseDate,
                // Gamification fields (nullable for backward compatibility)
                Points = gamification?.TotalPoints,
                Level = gamification?.CurrentLevel,
                Tier = gamification?.CurrentTier,
                CurrentStreak = gamification?.CurrentStreak,
                LongestStreak = gamification?.LongestStreak,
                RecentAchievements = gamification?.RecentAchievements?.Take(5).ToList()
            };
        }

        private HouseDto MapToHouseDto(House house)
        {
            if (house == null)
            {
                _logger.LogWarning("Attempted to map null house to DTO");
                throw new ArgumentNullException(nameof(house), "House cannot be null");
            }

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

                    // Gamification integration (after successful ticket creation)
                    if (_gamificationService != null)
                    {
                        try
                        {
                            // Award points: +10 per ticket
                            var pointsPerTicket = 10;
                            var totalPoints = tickets.Count * pointsPerTicket;
                            await _gamificationService.AwardPointsAsync(
                                request.UserId, 
                                totalPoints, 
                                "Ticket Purchase", 
                                request.PaymentId);

                            // Update streak
                            await _gamificationService.UpdateStreakAsync(request.UserId);

                            // Check if this is first entry
                            var totalEntries = await _context.LotteryTickets
                                .Where(t => t.UserId == request.UserId)
                                .CountAsync();
                            
                            if (totalEntries == tickets.Count)
                            {
                                // First entry - award bonus points
                                await _gamificationService.AwardPointsAsync(
                                    request.UserId, 
                                    50, 
                                    "First Entry Bonus", 
                                    request.PaymentId);
                            }

                            // Check achievements
                            await _gamificationService.CheckAchievementsAsync(
                                request.UserId, 
                                "EntryPurchase", 
                                new { ticketCount = totalEntries });
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail ticket creation if gamification fails
                            _logger.LogWarning(ex, "Gamification integration failed for user {UserId} after ticket creation", request.UserId);
                        }
                    }

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

        /// <summary>
        /// Transactions API response wrapper from Payment service
        /// Matches Payment.DTOs.ApiResponse format
        /// </summary>
        private class TransactionsApiResponse
        {
            public bool Success { get; set; }
            public List<TransactionDto>? Data { get; set; }
            public string? Message { get; set; }
            public PaymentErrorResponse? Error { get; set; }
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Transaction DTO from Payment service
        /// Matches Payment.DTOs.TransactionDto format
        /// </summary>
        private class TransactionDto
        {
            public Guid Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? ReferenceId { get; set; }
            public string? ProviderTransactionId { get; set; }
            public DateTime? ProcessedAt { get; set; }
            public DateTime CreatedAt { get; set; }
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

        public async Task<BulkFavoritesResponse> BulkAddFavoritesAsync(Guid userId, List<Guid> houseIds, CancellationToken cancellationToken = default)
        {
            var response = new BulkFavoritesResponse();

            if (houseIds.Count == 0)
            {
                response.TotalRequested = 0;
                return response;
            }

            // Remove duplicates first, then set TotalRequested to reflect unique count
            var uniqueHouseIds = houseIds.Distinct().ToList();
            response.TotalRequested = uniqueHouseIds.Count; // Set after deduplication
            
            if (uniqueHouseIds.Count != houseIds.Count)
            {
                _logger.LogInformation("Removed {DuplicateCount} duplicate house IDs from bulk add request for user {UserId}", 
                    houseIds.Count - uniqueHouseIds.Count, userId);
            }

            // Pre-validate all house IDs exist and are not soft-deleted (batch query for performance)
            var validHouseIds = await _context.Houses
                .AsNoTracking()
                .Where(h => uniqueHouseIds.Contains(h.Id) && h.DeletedAt == null)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);

            var invalidHouseIds = uniqueHouseIds.Except(validHouseIds).ToList();
            if (invalidHouseIds.Count > 0)
            {
                _logger.LogWarning("Bulk add request for user {UserId} contains {InvalidCount} invalid or deleted house IDs", userId, invalidHouseIds.Count);
                // Add invalid house IDs to errors immediately
                foreach (var invalidId in invalidHouseIds)
                {
                    response.Failed++;
                    response.Errors.Add(new BulkFavoriteError
                    {
                        HouseId = invalidId,
                        ErrorCode = "HOUSE_NOT_FOUND",
                        ErrorMessage = "House does not exist or has been deleted"
                    });
                }
            }

            // Only process valid house IDs
            foreach (var houseId in validHouseIds)
            {
                try
                {
                    var result = await AddHouseToFavoritesAsync(userId, houseId, cancellationToken);
                    if (result.Success)
                    {
                        response.Successful++;
                        response.SuccessfulHouseIds.Add(houseId);
                    }
                    else
                    {
                        response.Failed++;
                        response.Errors.Add(new BulkFavoriteError
                        {
                            HouseId = houseId,
                            ErrorCode = result.Error?.Code ?? "ADD_FAILED",
                            ErrorMessage = result.Error?.Message ?? "Failed to add house to favorites"
                        });
                    }
                }
                catch (Exception ex)
                {
                    response.Failed++;
                    response.Errors.Add(new BulkFavoriteError
                    {
                        HouseId = houseId,
                        ErrorCode = "EXCEPTION",
                        ErrorMessage = "An error occurred while processing this house"
                    });
                    _logger.LogError(ex, "Error adding house {HouseId} to favorites in bulk operation for user {UserId}", houseId, userId);
                }
            }

            return response;
        }

        public async Task<BulkFavoritesResponse> BulkRemoveFavoritesAsync(Guid userId, List<Guid> houseIds, CancellationToken cancellationToken = default)
        {
            var response = new BulkFavoritesResponse();

            if (houseIds.Count == 0)
            {
                response.TotalRequested = 0;
                return response;
            }

            // Remove duplicates first, then set TotalRequested to reflect unique count
            var uniqueHouseIds = houseIds.Distinct().ToList();
            response.TotalRequested = uniqueHouseIds.Count; // Set after deduplication
            
            if (uniqueHouseIds.Count != houseIds.Count)
            {
                _logger.LogInformation("Removed {DuplicateCount} duplicate house IDs from bulk remove request for user {UserId}", 
                    houseIds.Count - uniqueHouseIds.Count, userId);
            }

            // Pre-validate all house IDs exist and are not soft-deleted (batch query for performance)
            var validHouseIds = await _context.Houses
                .AsNoTracking()
                .Where(h => uniqueHouseIds.Contains(h.Id) && h.DeletedAt == null)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);

            var invalidHouseIds = uniqueHouseIds.Except(validHouseIds).ToList();
            if (invalidHouseIds.Count > 0)
            {
                _logger.LogWarning("Bulk remove request for user {UserId} contains {InvalidCount} invalid or deleted house IDs", userId, invalidHouseIds.Count);
                // Add invalid house IDs to errors immediately
                foreach (var invalidId in invalidHouseIds)
                {
                    response.Failed++;
                    response.Errors.Add(new BulkFavoriteError
                    {
                        HouseId = invalidId,
                        ErrorCode = "HOUSE_NOT_FOUND",
                        ErrorMessage = "House does not exist or has been deleted"
                    });
                }
            }

            // Only process valid house IDs
            foreach (var houseId in validHouseIds)
            {
                try
                {
                    var result = await RemoveHouseFromFavoritesAsync(userId, houseId, cancellationToken);
                    if (result.Success)
                    {
                        response.Successful++;
                        response.SuccessfulHouseIds.Add(houseId);
                    }
                    else
                    {
                        response.Failed++;
                        response.Errors.Add(new BulkFavoriteError
                        {
                            HouseId = houseId,
                            ErrorCode = result.Error?.Code ?? "REMOVE_FAILED",
                            ErrorMessage = result.Error?.Message ?? "Failed to remove house from favorites"
                        });
                    }
                }
                catch (Exception ex)
                {
                    response.Failed++;
                    response.Errors.Add(new BulkFavoriteError
                    {
                        HouseId = houseId,
                        ErrorCode = "EXCEPTION",
                        ErrorMessage = "An error occurred while processing this house"
                    });
                    _logger.LogError(ex, "Error removing house {HouseId} from favorites in bulk operation for user {UserId}", houseId, userId);
                }
            }

            return response;
        }

        public async Task<FavoritesAnalyticsDto> GetFavoritesAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            var analytics = new FavoritesAnalyticsDto();

            if (_authContext == null)
            {
                _logger.LogWarning("AuthContext not available for analytics");
                return analytics;
            }

            try
            {
                // Limit to prevent memory issues with large datasets (process max 10,000 user preferences)
                const int maxPreferencesToProcess = 10000;
                
                // Get user preferences in batches (optimized: no N+1, but with limit for memory protection)
                var allPreferences = await _authContext.UserPreferences
                    .AsNoTracking()
                    .Where(p => p.PreferencesJson != null && p.PreferencesJson != "{}")
                    .Take(maxPreferencesToProcess)
                    .ToListAsync(cancellationToken);
                
                if (allPreferences.Count >= maxPreferencesToProcess)
                {
                    _logger.LogWarning("Analytics query hit maximum preferences limit ({MaxLimit}). Results may be incomplete.", maxPreferencesToProcess);
                }

                var favoriteCounts = new Dictionary<Guid, int>();
                var uniqueUsers = new HashSet<Guid>();
                var favoritesByDate = new Dictionary<string, int>(); // Track favorites by date

                // Parse JSONB directly in memory (much faster than individual service calls)
                int processedCount = 0;
                foreach (var pref in allPreferences)
                {
                    // Check cancellation token periodically (every 100 items) to allow responsive cancellation
                    if (processedCount > 0 && processedCount % 100 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    
                    System.Text.Json.JsonDocument? jsonDoc = null;
                    try
                    {
                        if (string.IsNullOrEmpty(pref.PreferencesJson))
                            continue;

                        try
                        {
                            jsonDoc = System.Text.Json.JsonDocument.Parse(pref.PreferencesJson);
                        }
                        catch (System.Text.Json.JsonException jsonEx)
                        {
                            _logger.LogWarning(jsonEx, "Failed to parse JSONB for user {UserId}, skipping", pref.UserId);
                            continue; // Skip malformed JSON
                        }
                        
                        if (jsonDoc == null)
                            continue;
                            
                        if (jsonDoc.RootElement.TryGetProperty("lotteryPreferences", out var lotteryPrefs))
                        {
                            if (lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds))
                            {
                                if (favoriteIds.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    // OPTIMIZATION: Build DateAdded map once (O(n)) instead of O(n²) nested loop
                                    var dateAddedMap = new Dictionary<Guid, DateTime?>();
                                    var houseIdList = new List<Guid>();
                                    
                                    // Single pass through array to extract both house IDs and DateAdded
                                    int favoriteIndex = 0;
                                    foreach (var idElement in favoriteIds.EnumerateArray())
                                    {
                                        // Check cancellation token in nested loop (every 50 favorite items)
                                        if (favoriteIndex > 0 && favoriteIndex % 50 == 0)
                                        {
                                            cancellationToken.ThrowIfCancellationRequested();
                                        }
                                        favoriteIndex++;
                                        
                                        Guid? houseId = null;
                                        DateTime? dateAdded = null;
                                        
                                        if (idElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                                        {
                                            // Object format: { "houseId": "...", "dateAdded": "..." }
                                            if (idElement.TryGetProperty("houseId", out var houseIdProp) && 
                                                houseIdProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                            {
                                                if (Guid.TryParse(houseIdProp.GetString(), out var guid))
                                                {
                                                    houseId = guid;
                                                }
                                            }
                                            
                                            if (idElement.TryGetProperty("dateAdded", out var dateAddedProp) &&
                                                dateAddedProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                            {
                                                if (DateTime.TryParse(dateAddedProp.GetString(), out var parsedDate))
                                                {
                                                    dateAdded = parsedDate;
                                                }
                                            }
                                        }
                                        else if (idElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            // String format: just the GUID (no DateAdded available)
                                            if (Guid.TryParse(idElement.GetString(), out var guid))
                                            {
                                                houseId = guid;
                                            }
                                        }
                                        
                                        if (houseId.HasValue)
                                        {
                                            houseIdList.Add(houseId.Value);
                                            if (dateAdded.HasValue)
                                            {
                                                dateAddedMap[houseId.Value] = dateAdded.Value;
                                            }
                                        }
                                    }

                                    if (houseIdList.Count > 0)
                                    {
                                        uniqueUsers.Add(pref.UserId);
                                        analytics.TotalFavorites += houseIdList.Count;

                                        // Track favorites by date using pre-built map (O(1) lookup instead of O(n))
                                        int houseIndex = 0;
                                        foreach (var houseId in houseIdList)
                                        {
                                            // Check cancellation token in nested loop (every 50 houses)
                                            if (houseIndex > 0 && houseIndex % 50 == 0)
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();
                                            }
                                            houseIndex++;
                                            
                                            favoriteCounts.TryGetValue(houseId, out var count);
                                            favoriteCounts[houseId] = count + 1;
                                            
                                            // Get DateAdded from map (O(1) lookup)
                                            var dateAdded = dateAddedMap.GetValueOrDefault(houseId);
                                            
                                            // Fallback to UpdatedAt or CreatedAt if DateAdded is missing
                                            if (!dateAdded.HasValue)
                                            {
                                                dateAdded = pref.UpdatedAt;
                                            }
                                            
                                            if (dateAdded.HasValue)
                                            {
                                                var dateKey = dateAdded.Value.ToString("yyyy-MM-dd");
                                                favoritesByDate.TryGetValue(dateKey, out var dateCount);
                                                favoritesByDate[dateKey] = dateCount + 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing favorites for user {UserId} in analytics", pref.UserId);
                        // Continue with next user
                    }
                    finally
                    {
                        // Dispose JsonDocument to free memory
                        jsonDoc?.Dispose();
                        processedCount++;
                    }
                }

                // Populate FavoritesByDate (last 30 days for performance)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                analytics.FavoritesByDate = favoritesByDate
                    .Where(kvp => DateTime.TryParse(kvp.Key, out var date) && date >= thirtyDaysAgo)
                    .OrderByDescending(kvp => kvp.Key)
                    .Take(30)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                analytics.UniqueUsers = uniqueUsers.Count;

                // Get most favorited houses (batch query instead of N+1)
                var mostFavorited = favoriteCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .ToList();

                if (mostFavorited.Count > 0)
                {
                    var houseIds = mostFavorited.Select(kvp => kvp.Key).ToList();
                    var houses = await _context.Houses
                        .AsNoTracking()
                        .Where(h => houseIds.Contains(h.Id))
                        .ToDictionaryAsync(h => h.Id, h => h.Title, cancellationToken);

                    foreach (var (houseId, count) in mostFavorited)
                    {
                        if (houses.TryGetValue(houseId, out var houseTitle))
                        {
                            analytics.MostFavoritedHouses.Add(new MostFavoritedHouseDto
                            {
                                HouseId = houseId,
                                HouseTitle = houseTitle,
                                FavoriteCount = count
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating favorites analytics");
            }

            return analytics;
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

