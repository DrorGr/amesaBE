using AmesaBackend.Shared.Caching;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Tests.TestHelpers
{
    /// <summary>
    /// In-memory cache implementation for testing purposes
    /// Implements ICache interface to mock Redis cache behavior
    /// </summary>
    public class InMemoryCache : ICache
    {
        private readonly ConcurrentDictionary<string, (string Value, DateTime? Expiry)> _cache = new();
        private readonly ILogger<InMemoryCache>? _logger;

        public InMemoryCache(ILogger<InMemoryCache>? logger = null)
        {
            _logger = logger;
        }

        public Task<string?> GetRecordAsync(string cacheKey, bool isGlobal = false)
        {
            var key = GetKey(cacheKey, isGlobal);
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.Expiry == null || entry.Expiry > DateTime.UtcNow)
                {
                    return Task.FromResult<string?>(entry.Value);
                }
                // Expired, remove it
                _cache.TryRemove(key, out _);
            }
            return Task.FromResult<string?>(null);
        }

        public async Task<T?> GetRecordAsync<T>(string cacheKey, bool isGlobal = false)
        {
            var value = await GetRecordAsync(cacheKey, isGlobal);
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(value);
                return result;
            }
            catch
            {
                return default;
            }
        }

        public async Task<T?> GetValueTypeRecordAsync<T>(string cacheKey, bool isGlobal = false) where T : struct
        {
            var result = await GetRecordAsync<T>(cacheKey, isGlobal);
            return result;
        }

        public Task SetRecordAsync<T>(string cacheKey, T data, TimeSpan? absoluteExpiteTime = null, TimeSpan? unusedExpiteTime = null, bool isGlobal = false)
        {
            var key = GetKey(cacheKey, isGlobal);
            var json = JsonSerializer.Serialize(data);
            var expiry = absoluteExpiteTime.HasValue ? DateTime.UtcNow.Add(absoluteExpiteTime.Value) : (DateTime?)null;
            _cache[key] = (json, expiry);
            return Task.CompletedTask;
        }

        public Task RemoveRecordAsync(string cacheKey, bool isGlobal = false)
        {
            var key = GetKey(cacheKey, isGlobal);
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ClearAllCache()
        {
            _cache.Clear();
            return Task.FromResult(true);
        }

        public T? GetRecord<T>(string cacheKey, bool isGlobal = false)
        {
            // Note: This synchronous method should only be used in test contexts where async is not possible
            // Use Task.Run to avoid deadlocks in async contexts
            return Task.Run(async () => await GetRecordAsync<T>(cacheKey, isGlobal)).Result;
        }

        public async Task BatchSet<T>(Dictionary<string, T> data, bool isGlobal = false)
        {
            var tasks = data.Select(kvp => SetRecordAsync(kvp.Key, kvp.Value, isGlobal: isGlobal));
            await Task.WhenAll(tasks);
        }

        public Task<long> RemoveByControllerName(string controllerName)
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(controllerName, StringComparison.OrdinalIgnoreCase)).ToList();
            var count = 0L;
            foreach (var key in keysToRemove)
            {
                if (_cache.TryRemove(key, out _))
                {
                    count++;
                }
            }
            return Task.FromResult(count);
        }

        public Task<bool> DeleteByRegex(string regex)
        {
            // Simple pattern matching - convert Redis-style pattern to regex
            var pattern = regex.Replace("*", ".*").Replace("?", ".");
            var keysToRemove = _cache.Keys.Where(k => System.Text.RegularExpressions.Regex.IsMatch(k, pattern)).ToList();
            var removed = false;
            foreach (var key in keysToRemove)
            {
                if (_cache.TryRemove(key, out _))
                {
                    removed = true;
                }
            }
            return Task.FromResult(removed);
        }

        private string GetKey(string cacheKey, bool isGlobal)
        {
            return isGlobal ? $"global:{cacheKey}" : cacheKey;
        }

        public void Dispose()
        {
            _cache.Clear();
        }
    }
}

