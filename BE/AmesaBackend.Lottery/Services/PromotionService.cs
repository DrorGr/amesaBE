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
    /// <summary>
    /// Service for managing lottery promotions, discounts, and special offers.
    /// Handles creation, validation, application, and analytics for promotions.
    /// </summary>
    public class PromotionService : IPromotionService
    {
        private readonly LotteryDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICache? _cache;
        private readonly ILogger<PromotionService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PromotionService"/> class.
        /// </summary>
        /// <param name="context">Database context for lottery data access.</param>
        /// <param name="eventPublisher">Event publisher for publishing promotion-related events.</param>
        /// <param name="logger">Logger instance for logging operations.</param>
        /// <param name="cache">Optional cache service for caching promotion data.</param>
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

            return new PromotionValidationResponse
            {
                IsValid = true,
                Promotion = MapToDto(promotion),
                DiscountAmount = discountAmount,
                Message = "Promotion is valid"
            };
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
                
                // Execute a lock query first to acquire the row lock
                // This prevents other transactions from modifying the promotion
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM amesa_admin.promotions WHERE UPPER(code) = {0} FOR UPDATE",
                    promotionCodeUpper);
                
                // Now query the locked row - EF Core will map columns automatically
                // The row remains locked until the transaction commits
                var promotion = await _context.Promotions
                    .Where(p => p.Code != null && p.Code.ToUpper() == promotionCodeUpper)
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
                    DiscountAmount = userPromotion.DiscountAmount ?? 0,
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
                .Include(up => up.Promotion)
                .Where(up => up.UserId == userId)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();

            return userPromotions.Select(up => new PromotionUsageDto
            {
                Id = up.Id,
                PromotionId = up.PromotionId,
                PromotionCode = up.Promotion?.Code ?? string.Empty,
                UserId = up.UserId,
                TransactionId = up.TransactionId,
                DiscountAmount = up.DiscountAmount ?? 0,
                UsedAt = up.UsedAt
            }).ToList();
        }

        public async Task<List<PromotionDto>> GetAvailablePromotionsAsync(Guid userId, Guid? houseId)
        {
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

            return promotions.Select(MapToDto).ToList();
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

            var uniqueUsers = usages.Select(u => u.UserId).Distinct().Count();
            
            return new PromotionAnalyticsDto
            {
                PromotionId = promotion.Id,
                Code = promotion.Code ?? string.Empty,
                Name = promotion.Title, // Map Title → Name
                TotalUsage = totalUsage,
                UniqueUsers = uniqueUsers,
                TotalDiscountAmount = totalDiscountGiven,
                AverageDiscountAmount = averageDiscount,
                FirstUsedAt = firstUsed,
                LastUsedAt = lastUsed
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
        /// Calculates the discount amount based on promotion type and value.
        /// Handles percentage-based, fixed amount, and special promotion types.
        /// </summary>
        /// <param name="promotion">The promotion entity containing discount rules.</param>
        /// <param name="purchaseAmount">The purchase amount before discount.</param>
        /// <returns>The calculated discount amount. Returns 0 if minimum purchase amount is not met.</returns>
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
        /// Calculates a representative discount amount for event publishing.
        /// Uses a simplified calculation for event notifications.
        /// </summary>
        /// <param name="promotion">The promotion entity.</param>
        /// <returns>A representative discount amount for the event.</returns>
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
        /// Maps a Promotion entity to a PromotionDto.
        /// Handles property name mappings (e.g., Title → Name, MinPurchaseAmount → MinAmount).
        /// </summary>
        /// <param name="promotion">The promotion entity to map.</param>
        /// <returns>A PromotionDto containing the mapped promotion data.</returns>
        private PromotionDto MapToDto(Promotion promotion)
        {
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




