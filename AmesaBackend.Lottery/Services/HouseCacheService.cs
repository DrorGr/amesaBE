using AmesaBackend.Shared.Caching;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services;

public interface IHouseCacheService
{
    Task InvalidateHouseCachesAsync();
}

public class HouseCacheService : IHouseCacheService
{
    private readonly ICache _cache;
    private readonly ILogger<HouseCacheService> _logger;

    public HouseCacheService(ICache cache, ILogger<HouseCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger;
    }

    /// <summary>
    /// Invalidates all house list caches when a house is created, updated, or deleted
    /// </summary>
    public async Task InvalidateHouseCachesAsync()
    {
        try
        {
            // Delete all cache keys matching the pattern "houses_*"
            // This ensures all cached house lists are invalidated when a house is modified
            await _cache.DeleteByRegex("houses_*");
            _logger.LogDebug("Invalidated all house list caches");
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            // Cache invalidation is non-critical - worst case, stale data is served briefly
            _logger.LogWarning(ex, "Error invalidating house caches (non-critical)");
        }
    }
}






