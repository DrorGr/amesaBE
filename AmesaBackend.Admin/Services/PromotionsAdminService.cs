using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Admin.Services;

public interface IPromotionsAdminService
{
    Task<PagedResult<AdminPromotionDto>> GetPromotionsAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);
    Task<AdminPromotionDto> SavePromotionAsync(SaveAdminPromotionRequest request);
    Task DisablePromotionAsync(Guid id);
}

public sealed class PromotionsAdminService : IPromotionsAdminService
{
    private readonly LotteryDbContext _context;
    private readonly IAdminPermissionService _permissions;
    private readonly IAdminAuditService _audit;
    private readonly ILogger<PromotionsAdminService> _logger;

    public PromotionsAdminService(
        LotteryDbContext context,
        IAdminPermissionService permissions,
        IAdminAuditService audit,
        ILogger<PromotionsAdminService> logger)
    {
        _context = context;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PagedResult<AdminPromotionDto>> GetPromotionsAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null)
    {
        await RequireEngagementAccessAsync();

        var query = _context.Promotions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Title.ToLower().Contains(normalized) ||
                (p.Code != null && p.Code.ToLower().Contains(normalized)) ||
                (p.Description != null && p.Description.ToLower().Contains(normalized)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var promotions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var promotionIds = promotions.Select(p => p.Id).ToList();
        var discountTotals = await _context.UserPromotions
            .AsNoTracking()
            .Where(u => promotionIds.Contains(u.PromotionId))
            .GroupBy(u => u.PromotionId)
            .Select(g => new { PromotionId = g.Key, Total = g.Sum(u => u.DiscountAmount ?? 0) })
            .ToDictionaryAsync(x => x.PromotionId, x => x.Total);

        return new PagedResult<AdminPromotionDto>
        {
            Items = promotions.Select(p => Map(p, discountTotals.GetValueOrDefault(p.Id))).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<AdminPromotionDto> SavePromotionAsync(SaveAdminPromotionRequest request)
    {
        await RequireEngagementAccessAsync();

        ValidatePromotion(request);
        var applicableHouses = ParseHouseIds(request.ApplicableHouseIds);

        Promotion promotion;
        var isCreate = !request.Id.HasValue;
        if (isCreate)
        {
            var codeExists = await _context.Promotions
                .AnyAsync(p => p.Code != null && p.Code.ToUpper() == request.Code.Trim().ToUpper());

            if (codeExists)
            {
                throw new InvalidOperationException($"Promotion code '{request.Code}' already exists.");
            }

            promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Promotions.Add(promotion);
        }
        else
        {
            promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == request.Id.Value)
                ?? throw new KeyNotFoundException("Promotion not found.");
        }

        promotion.Code = request.Code.Trim().ToUpperInvariant();
        promotion.Title = request.Name.Trim();
        promotion.Description = request.Description;
        promotion.Type = request.Type.Trim();
        promotion.Value = request.Value;
        promotion.ValueType = request.ValueType;
        promotion.MinPurchaseAmount = request.MinAmount;
        promotion.MaxDiscountAmount = request.MaxDiscount;
        promotion.UsageLimit = request.UsageLimit;
        promotion.IsActive = request.IsActive;
        promotion.StartDate = request.StartDate;
        promotion.EndDate = request.EndDate;
        promotion.ApplicableHouses = applicableHouses;
        promotion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin {Action} promotion {PromotionId}", isCreate ? "created" : "updated", promotion.Id);
        await _audit.LogAsync(isCreate ? "promotion.created" : "promotion.updated", "promotion", promotion.Id, new
        {
            promotion.Code,
            promotion.Title,
            promotion.IsActive
        });

        return Map(promotion, await GetTotalDiscountAsync(promotion.Id));
    }

    public async Task DisablePromotionAsync(Guid id)
    {
        await RequireEngagementAccessAsync();

        var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Promotion not found.");

        promotion.IsActive = false;
        promotion.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _audit.LogAsync("promotion.disabled", "promotion", id, new { promotion.Code, promotion.Title });
    }

    private async Task RequireEngagementAccessAsync()
    {
        if (await _permissions.HasPermissionAsync(AdminPermissionNames.SettingsManage) ||
            await _permissions.HasPermissionAsync(AdminPermissionNames.AuditRead))
        {
            return;
        }

        throw new UnauthorizedAccessException($"Admin permission required: {AdminPermissionNames.SettingsManage} or {AdminPermissionNames.AuditRead}");
    }

    private async Task<decimal> GetTotalDiscountAsync(Guid promotionId)
    {
        return await _context.UserPromotions
            .AsNoTracking()
            .Where(u => u.PromotionId == promotionId)
            .SumAsync(u => u.DiscountAmount ?? 0);
    }

    private static AdminPromotionDto Map(Promotion promotion, decimal totalDiscountGiven)
    {
        return new AdminPromotionDto
        {
            Id = promotion.Id,
            Code = promotion.Code ?? string.Empty,
            Name = promotion.Title,
            Description = promotion.Description,
            Type = promotion.Type,
            Value = promotion.Value,
            ValueType = promotion.ValueType,
            MinAmount = promotion.MinPurchaseAmount,
            MaxDiscount = promotion.MaxDiscountAmount,
            UsageLimit = promotion.UsageLimit,
            UsageCount = promotion.UsageCount,
            IsActive = promotion.IsActive,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            ApplicableHouses = promotion.ApplicableHouses,
            CreatedAt = promotion.CreatedAt,
            UpdatedAt = promotion.UpdatedAt,
            TotalDiscountGiven = totalDiscountGiven
        };
    }

    private static void ValidatePromotion(SaveAdminPromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("Promotion code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Promotion name is required.");
        }

        if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate.Value < request.StartDate.Value)
        {
            throw new InvalidOperationException("End date must be after start date.");
        }
    }

    private static Guid[]? ParseHouseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var houseIds = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        return houseIds.Length == 0 ? null : houseIds;
    }
}
