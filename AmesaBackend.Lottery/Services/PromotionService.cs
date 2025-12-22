using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Caching;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly LotteryDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICache? _cache;
        private readonly ILogger<PromotionService> _logger;
        private const string ActivePromotionsCacheKey = "promotions:active";
        private const int ActivePromotionsCacheMinutes = 5;
        private const int ValidationCacheMinutes = 1;

        public PromotionService(
            LotteryDbContext context,
            IEventPublisher eventPublisher,
            ILogger<PromotionService> logger,
            ICache? cache = null)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PagedResponse<PromotionDto>> GetPromotionsAsync(PromotionSearchParams searchParams)
        {
            var query = _context.Promotions.AsQueryable();

            // Apply filters
            if (searchParams.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == searchParams.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(searchParams.Type))
            {
                query = query.Where(p => p.Type == searchParams.Type);
            }

            if (!string.IsNullOrEmpty(searchParams.Search))
            {
                var searchLower = searchParams.Search.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(searchLower) ||
                    (p.Code != null && p.Code.ToLower().Contains(searchLower)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
            }

            if (searchParams.StartDate.HasValue)
            {
                query = query.Where(p => p.StartDate == null || p.StartDate <= searchParams.StartDate.Value);
            }

            if (searchParams.EndDate.HasValue)
            {
                query = query.Where(p => p.EndDate == null || p.EndDate >= searchParams.EndDate.Value);
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply pagination
            var promotions = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((searchParams.Page - 1) * searchParams.Limit)
                .Take(searchParams.Limit)
                .ToListAsync();

            var items = promotions.Select(MapToDto).ToList();

            return new PagedResponse<PromotionDto>
            {
                Items = items,
                Page = searchParams.Page,
                Limit = searchParams.Limit,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)searchParams.Limit),
                HasNext = searchParams.Page * searchParams.Limit < total,
                HasPrevious = searchParams.Page > 1
            };
        }

        public async Task<PromotionDto?> GetPromotionByIdAsync(Guid id)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id);

            return promotion != null ? MapToDto(promotion) : null;
        }

        public async Task<PromotionDto?> GetPromotionByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code != null && p.Code.ToUpper() == code.ToUpper());

            return promotion != null ? MapToDto(promotion) : null;
        }

        public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionRequest request, Guid? createdBy)
        {
            // Check code uniqueness (case-insensitive)
            var existingCode = await _context.Promotions
                .AnyAsync(p => p.Code != null && p.Code.ToUpper() == request.Code.ToUpper());

            if (existingCode)
            {
                throw new InvalidOperationException($"Promotion code '{request.Code}' already exists");
            }

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Title = request.Name, // Map name → Title
                Description = request.Description,
                Type = request.Type,
                Value = request.Value,
                ValueType = request.ValueType,
                MinPurchaseAmount = request.MinAmount, // Map minAmount → MinPurchaseAmount
                MaxDiscountAmount = request.MaxDiscount, // Map maxDiscount → MaxDiscountAmount
                UsageLimit = request.UsageLimit,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ApplicableHouses = request.ApplicableHouses,
                IsActive = request.IsActive,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            // Publish PromotionCreatedEvent
            var discountAmount = CalculateDiscountAmountForEvent(promotion);
            await _eventPublisher.PublishAsync(new PromotionCreatedEvent
            {
                PromotionId = promotion.Id,
                Code = promotion.Code ?? string.Empty,
                DiscountAmount = discountAmount
            });

            _logger.LogInformation("Promotion created: {PromotionId} - {Code}", promotion.Id, promotion.Code);

            // Invalidate promotion caches
            await InvalidatePromotionCachesAsync();

            return MapToDto(promotion);
        }

        public async Task<PromotionDto> UpdatePromotionAsync(Guid id, UpdatePromotionRequest request)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                throw new KeyNotFoundException($"Promotion with ID {id} not found");
            }

            // Check code uniqueness if code is being changed
            if (!string.IsNullOrEmpty(request.Name) && request.Name != promotion.Title)
            {
                promotion.Title = request.Name; // Map name → Title
            }

            if (request.Description != null)
            {
                promotion.Description = request.Description;
            }

            if (!string.IsNullOrEmpty(request.Type))
            {
                promotion.Type = request.Type;
            }

            if (request.Value.HasValue)
            {
                promotion.Value = request.Value;
            }

            if (request.ValueType != null)
            {
                promotion.ValueType = request.ValueType;
            }

            if (request.MinAmount.HasValue)
            {
                promotion.MinPurchaseAmount = request.MinAmount; // Map minAmount → MinPurchaseAmount
            }

            if (request.MaxDiscount.HasValue)
            {
                promotion.MaxDiscountAmount = request.MaxDiscount; // Map maxDiscount → MaxDiscountAmount
            }

            if (request.UsageLimit.HasValue)
            {
                promotion.UsageLimit = request.UsageLimit;
            }

            if (request.IsActive.HasValue)
            {
                promotion.IsActive = request.IsActive.Value;
            }

            if (request.StartDate.HasValue)
            {
                promotion.StartDate = request.StartDate;
            }

            if (request.EndDate.HasValue)
            {
                promotion.EndDate = request.EndDate;
            }

            if (request.ApplicableHouses != null)
            {
                promotion.ApplicableHouses = request.ApplicableHouses;
            }

            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Promotion updated: {PromotionId}", promotion.Id);

            // Invalidate promotion caches
            await InvalidatePromotionCachesAsync();

            return MapToDto(promotion);
        }

        public async Task<bool> DeletePromotionAsync(Guid id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return false;
            }

            // Soft delete - set is_active = false
            promotion.IsActive = false;
            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Promotion soft deleted: {PromotionId}", promotion.Id);

            // Invalidate promotion caches
            await InvalidatePromotionCachesAsync();

            return true;
        }

        public async Task<PromotionValidationResponse> ValidatePromotionAsync(ValidatePromotionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion code is required",
                    ErrorCode = "PROMOTION_CODE_INVALID"
                };
            }

            // Check cache first
            var cacheKey = $"promotion:validate:{request.Code.ToUpper()}:{request.UserId}:{request.HouseId}:{request.Amount}";
            if (_cache != null)
            {
                var cached = await _cache.GetRecordAsync<PromotionValidationResponse>(cacheKey);
                if (cached != null)
                {
                    _logger.LogDebug("Promotion validation cache hit for code {Code}", request.Code);
                    return cached;
                }
            }

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code != null && p.Code.ToUpper() == request.Code.ToUpper());

            if (promotion == null)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion code not found",
                    ErrorCode = "PROMOTION_NOT_FOUND"
                };
            }

            // Check if promotion is active
            if (!promotion.IsActive)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion is not active",
                    ErrorCode = "PROMOTION_INACTIVE"
                };
            }

            // Check date range
            var now = DateTime.UtcNow;
            if (promotion.StartDate.HasValue && now < promotion.StartDate.Value)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion has not started yet",
                    ErrorCode = "PROMOTION_NOT_STARTED"
                };
            }

            if (promotion.EndDate.HasValue && now > promotion.EndDate.Value)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion has expired",
                    ErrorCode = "PROMOTION_EXPIRED"
                };
            }

            // Check usage limit
            if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion usage limit reached",
                    ErrorCode = "PROMOTION_USAGE_LIMIT_REACHED"
                };
            }

            // Check minimum purchase amount
            if (promotion.MinPurchaseAmount.HasValue && request.Amount < promotion.MinPurchaseAmount.Value)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = $"Minimum purchase amount of {promotion.MinPurchaseAmount.Value:C} not met",
                    ErrorCode = "PROMOTION_MIN_PURCHASE_NOT_MET"
                };
            }

            // Check house applicability
            if (promotion.ApplicableHouses != null && promotion.ApplicableHouses.Length > 0)
            {
                if (!request.HouseId.HasValue || !promotion.ApplicableHouses.Contains(request.HouseId.Value))
                {
                    return new PromotionValidationResponse
                    {
                        IsValid = false,
                        Message = "Promotion is not applicable for this house",
                        ErrorCode = "PROMOTION_INVALID_FOR_HOUSE"
                    };
                }
            }

            // Check if user has already used this promotion (one-time use check)
            var hasUsed = await _context.UserPromotions
                .AnyAsync(up => up.UserId == request.UserId && up.PromotionId == promotion.Id);

            if (hasUsed)
            {
                return new PromotionValidationResponse
                {
                    IsValid = false,
                    Message = "Promotion has already been used",
                    ErrorCode = "PROMOTION_ALREADY_USED"
                };
            }

            // Calculate discount amount
            var discountAmount = CalculateDiscount(promotion, request.Amount);

            var response = new PromotionValidationResponse
            {
                IsValid = true,
                Promotion = MapToDto(promotion),
                DiscountAmount = discountAmount,
                Message = "Promotion is valid"
            };

            // Cache validation result (TTL: 1 minute for validation results)
            if (_cache != null)
            {
                await _cache.SetRecordAsync(cacheKey, response, TimeSpan.FromMinutes(ValidationCacheMinutes));
            }

            return response;
        }

        public async Task<PromotionUsageDto> ApplyPromotionAsync(ApplyPromotionRequest request)
        {
            // Use transaction with Serializable isolation level to prevent race conditions
            var isolationLevel = IsolationLevel.Serializable;
            using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
            try
            {
                // Lock promotion row for update to prevent concurrent modifications
                // Use raw SQL with FOR UPDATE to lock the row, then query normally
                // This ensures the row is locked before we read it, preventing race conditions
                var promotionCodeUpper = request.Code.ToUpper();
                
                // Use EF Core FromSqlRaw with parameters for row-level locking (FOR UPDATE)
                // Parameters are safely parameterized by EF Core, preventing SQL injection
                // Note: {0} syntax is parameterized, not string interpolation
                var promotion = await _context.Promotions
                    .FromSqlRaw(
                        "SELECT * FROM amesa_admin.promotions WHERE UPPER(code) = {0} FOR UPDATE",
                        promotionCodeUpper)
                    .AsTracking() // Ensure entity is tracked for updates
                    .FirstOrDefaultAsync();

                if (promotion == null)
                {
                    throw new KeyNotFoundException($"Promotion with code '{request.Code}' not found");
                }

                // Re-validate with locked promotion (race condition protection)
                var now = DateTime.UtcNow;
                
                // Check if promotion is active
                if (!promotion.IsActive)
                {
                    throw new InvalidOperationException("Promotion is not active");
                }

                // Check date range
                if (promotion.StartDate.HasValue && now < promotion.StartDate.Value)
                {
                    throw new InvalidOperationException("Promotion has not started yet");
                }

                if (promotion.EndDate.HasValue && now > promotion.EndDate.Value)
                {
                    throw new InvalidOperationException("Promotion has expired");
                }

                // Check usage limit (with locked row)
                if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value)
                {
                    throw new InvalidOperationException("Promotion usage limit reached");
                }

                // Check minimum purchase amount
                if (promotion.MinPurchaseAmount.HasValue && request.Amount < promotion.MinPurchaseAmount.Value)
                {
                    throw new InvalidOperationException($"Minimum purchase amount of {promotion.MinPurchaseAmount.Value:C} not met");
                }

                // Check house applicability
                if (promotion.ApplicableHouses != null && promotion.ApplicableHouses.Length > 0)
                {
                    if (!request.HouseId.HasValue || !promotion.ApplicableHouses.Contains(request.HouseId.Value))
                    {
                        throw new InvalidOperationException("Promotion is not applicable for this house");
                    }
                }

                // Check if user has already used this promotion (with locked row)
                var hasUsed = await _context.UserPromotions
                    .AnyAsync(up => up.UserId == request.UserId && up.PromotionId == promotion.Id);

                if (hasUsed)
                {
                    throw new InvalidOperationException("Promotion has already been used");
                }

                // Calculate and validate discount amount
                var calculatedDiscount = CalculateDiscount(promotion, request.Amount);
                
                // Allow small rounding differences (0.01) but validate discount amount
                if (Math.Abs(calculatedDiscount - request.DiscountAmount) > 0.01m)
                {
                    _logger.LogWarning(
                        "Discount amount mismatch for promotion {PromotionCode}: Expected {Expected}, Got {Got}",
                        request.Code, calculatedDiscount, request.DiscountAmount);
                    throw new InvalidOperationException(
                        $"Discount amount mismatch. Expected {calculatedDiscount:C}, got {request.DiscountAmount:C}");
                }

                // Create user promotion record
                var userPromotion = new UserPromotion
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    PromotionId = promotion.Id,
                    TransactionId = request.TransactionId,
                    DiscountAmount = request.DiscountAmount,
                    UsedAt = DateTime.UtcNow
                };

                _context.UserPromotions.Add(userPromotion);

                // Increment usage count
                promotion.UsageCount++;
                promotion.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Promotion applied: {PromotionId} by user {UserId} for transaction {TransactionId}, discount {Discount}",
                    promotion.Id, request.UserId, request.TransactionId, request.DiscountAmount);

                return new PromotionUsageDto
                {
                    Id = userPromotion.Id,
                    PromotionId = userPromotion.PromotionId,
                    UserId = userPromotion.UserId,
                    TransactionId = userPromotion.TransactionId,
                    DiscountAmount = userPromotion.DiscountAmount,
                    UsedAt = userPromotion.UsedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PromotionUsageDto>> GetUserPromotionHistoryAsync(Guid userId)
        {
            var userPromotions = await _context.UserPromotions
                .Where(up => up.UserId == userId)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();

            return userPromotions.Select(up => new PromotionUsageDto
            {
                Id = up.Id,
                PromotionId = up.PromotionId,
                UserId = up.UserId,
                TransactionId = up.TransactionId,
                DiscountAmount = up.DiscountAmount,
                UsedAt = up.UsedAt
            }).ToList();
        }

        public async Task<List<PromotionDto>> GetAvailablePromotionsAsync(Guid userId, Guid? houseId)
        {
            try
            {
                // Check cache first (cache key includes userId and houseId for user-specific results)
                var cacheKey = $"promotions:available:{userId}:{houseId?.ToString() ?? "all"}";
                if (_cache != null)
                {
                    try
                    {
                        var cached = await _cache.GetRecordAsync<List<PromotionDto>>(cacheKey);
                        if (cached != null)
                        {
                            _logger.LogDebug("Available promotions cache hit for user {UserId}, house {HouseId}", userId, houseId);
                            return cached;
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "Failed to get available promotions from cache for user {UserId}, falling back to database", userId);
                    }
                }

                var now = DateTime.UtcNow;

                var query = _context.Promotions
                    .Where(p => p.IsActive &&
                               (p.StartDate == null || p.StartDate <= now) &&
                               (p.EndDate == null || p.EndDate >= now) &&
                               (!p.UsageLimit.HasValue || p.UsageCount < p.UsageLimit.Value));

                // Filter by house if specified
                if (houseId.HasValue)
                {
                    query = query.Where(p =>
                        p.ApplicableHouses == null ||
                        p.ApplicableHouses.Length == 0 ||
                        p.ApplicableHouses.Contains(houseId.Value));
                }

                // Exclude promotions user has already used
                var usedPromotionIds = await _context.UserPromotions
                    .Where(up => up.UserId == userId)
                    .Select(up => up.PromotionId)
                    .ToListAsync();

                query = query.Where(p => !usedPromotionIds.Contains(p.Id));

                var promotions = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var result = promotions
                    .Where(p => p != null)
                    .Select(MapToDto)
                    .Where(dto => dto != null)
                    .ToList();

                // Cache result (TTL: 5 minutes for available promotions)
                if (_cache != null && result != null)
                {
                    try
                    {
                        await _cache.SetRecordAsync(cacheKey, result, TimeSpan.FromMinutes(ActivePromotionsCacheMinutes));
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "Failed to cache available promotions for user {UserId}", userId);
                    }
                }

                return result ?? new List<PromotionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available promotions for user {UserId}, house {HouseId}", userId, houseId);
                throw; // Re-throw to be handled by controller
            }
        }

        public async Task<PromotionAnalyticsDto> GetPromotionUsageStatsAsync(Guid promotionId)
        {
            var promotion = await _context.Promotions.FindAsync(promotionId);
            if (promotion == null)
            {
                throw new KeyNotFoundException($"Promotion with ID {promotionId} not found");
            }

            var usages = await _context.UserPromotions
                .Where(up => up.PromotionId == promotionId)
                .ToListAsync();

            var totalUsage = usages.Count;
            var totalDiscountGiven = usages.Sum(u => u.DiscountAmount ?? 0);
            var averageDiscount = totalUsage > 0 ? totalDiscountGiven / totalUsage : 0;
            var firstUsed = usages.Any() ? usages.Min(u => u.UsedAt) : (DateTime?)null;
            var lastUsed = usages.Any() ? usages.Max(u => u.UsedAt) : (DateTime?)null;

            return new PromotionAnalyticsDto
            {
                PromotionId = promotion.Id,
                Code = promotion.Code ?? string.Empty,
                Name = promotion.Title, // Map Title → Name
                TotalUsage = totalUsage,
                TotalDiscountGiven = totalDiscountGiven,
                AverageDiscount = averageDiscount,
                FirstUsed = firstUsed,
                LastUsed = lastUsed
            };
        }

        public async Task<List<PromotionAnalyticsDto>> GetPromotionAnalyticsAsync(PromotionSearchParams? searchParams)
        {
            var query = _context.Promotions.AsQueryable();

            if (searchParams != null)
            {
                if (searchParams.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == searchParams.IsActive.Value);
                }

                if (!string.IsNullOrEmpty(searchParams.Type))
                {
                    query = query.Where(p => p.Type == searchParams.Type);
                }
            }

            var promotions = await query.ToListAsync();
            var analytics = new List<PromotionAnalyticsDto>();

            foreach (var promotion in promotions)
            {
                var stats = await GetPromotionUsageStatsAsync(promotion.Id);
                analytics.Add(stats);
            }

            return analytics.OrderByDescending(a => a.TotalUsage).ToList();
        }

        /// <summary>
        /// Calculate discount amount based on promotion type and value
        /// </summary>
        private decimal CalculateDiscount(Promotion promotion, decimal purchaseAmount)
        {
            // Check minimum purchase amount
            if (promotion.MinPurchaseAmount.HasValue &&
                purchaseAmount < promotion.MinPurchaseAmount.Value)
            {
                return 0;
            }

            decimal discount = 0;

            switch (promotion.ValueType?.ToLower())
            {
                case "percentage":
                    if (promotion.Value.HasValue)
                    {
                        discount = (purchaseAmount * promotion.Value.Value) / 100;
                        if (promotion.MaxDiscountAmount.HasValue)
                        {
                            discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);
                        }
                    }
                    break;

                case "fixed_amount":
                case "fixed":
                    discount = promotion.Value ?? 0;
                    break;

                case "free_tickets":
                    // Special handling - add tickets instead of discount
                    // This would be handled separately in ticket creation
                    discount = 0;
                    break;

                default:
                    // Default to fixed amount if value type not specified
                    discount = promotion.Value ?? 0;
                    break;
            }

            // Discount cannot exceed purchase amount
            return Math.Min(discount, purchaseAmount);
        }

        /// <summary>
        /// Calculate discount amount for event publishing (simplified version)
        /// </summary>
        private decimal CalculateDiscountAmountForEvent(Promotion promotion)
        {
            // For event, we use a representative discount amount
            // This is typically the value itself (percentage or fixed)
            if (promotion.ValueType?.ToLower() == "percentage")
            {
                // For percentage, use the percentage value itself
                return promotion.Value ?? 0;
            }
            else
            {
                // For fixed amount, use the value
                return promotion.Value ?? 0;
            }
        }

        /// <summary>
        /// Invalidate all promotion-related caches
        /// </summary>
        /// <summary>
        /// Invalidates promotion-related caches after create/update/delete operations.
        /// 
        /// Cache Invalidation Strategy:
        /// - Active promotions list cache: Explicitly invalidated (immediate)
        /// - Validation result caches: Natural expiration (1 minute TTL) - acceptable for validation results
        /// - Available promotions caches: Natural expiration (5 minutes TTL) - acceptable for user-specific lists
        /// 
        /// Rationale:
        /// - Validation caches are short-lived (1 min) and user-specific, so natural expiration is acceptable
        /// - Available promotions caches are user-specific and house-specific, making pattern-based invalidation
        ///   complex without Redis pattern matching support. Natural expiration (5 min) provides acceptable
        ///   freshness while maintaining performance.
        /// - Active promotions list is global and frequently accessed, so explicit invalidation ensures consistency
        /// 
        /// Future Enhancement:
        /// If Redis pattern-based deletion is needed, consider using SCAN with pattern matching or implementing
        /// cache versioning/timestamp in cache keys for more aggressive invalidation.
        /// </summary>
        private async Task InvalidatePromotionCachesAsync()
        {
            if (_cache == null)
            {
                return;
            }

            try
            {
                // Invalidate active promotions cache (global, explicitly invalidated for consistency)
                await _cache.RemoveRecordAsync(ActivePromotionsCacheKey);
                
                // Note: Individual validation caches will expire naturally (1 minute TTL)
                // Available promotions caches are user-specific and will expire naturally (5 minutes TTL)
                // For more aggressive invalidation, we could use pattern-based deletion if Redis supports it
                _logger.LogDebug("Promotion caches invalidated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating promotion caches");
            }
        }

        /// <summary>
        /// Map Promotion entity to PromotionDto (Title → Name mapping)
        /// </summary>
        private PromotionDto MapToDto(Promotion promotion)
        {
            if (promotion == null)
            {
                _logger.LogWarning("Attempted to map null promotion to DTO");
                throw new ArgumentNullException(nameof(promotion), "Promotion cannot be null");
            }

            return new PromotionDto
            {
                Id = promotion.Id,
                Code = promotion.Code ?? string.Empty,
                Name = promotion.Title, // Map Title → Name
                Description = promotion.Description,
                Type = promotion.Type,
                Value = promotion.Value,
                MinAmount = promotion.MinPurchaseAmount, // Map MinPurchaseAmount → MinAmount
                MaxDiscount = promotion.MaxDiscountAmount, // Map MaxDiscountAmount → MaxDiscount
                UsageLimit = promotion.UsageLimit,
                UsageCount = promotion.UsageCount,
                IsActive = promotion.IsActive,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                ApplicableHouses = promotion.ApplicableHouses,
                CreatedAt = promotion.CreatedAt,
                UpdatedAt = promotion.UpdatedAt
            };
        }
    }
}

