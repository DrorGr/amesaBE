using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using IServer = StackExchange.Redis.IServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AmesaBackend.Shared.Caching
{
    public class StackRedisCache : ICache
    {
        private readonly ILogger<StackRedisCache> _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly CacheConfig _config;
        private readonly string[] _connection;

        private bool _disposedValue;

        public StackRedisCache(
            ILogger<StackRedisCache> logger,
            IConnectionMultiplexer connectionMultiplexer,
            IOptions<CacheConfig> config)
        {
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _config = config.Value;
            _connection = _config.RedisConnection.Split(",");
        }

        private IDatabase RedisDb => _connectionMultiplexer.GetDatabase();
        private IServer RedisServer => _connectionMultiplexer.GetServer(_connection.First());

        private static JsonSerializerSettings GetCamelCaseSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
        }

        public async Task BatchSet<T>(Dictionary<string, T> data, bool isGlobal = false)
        {
            try
            {
                var settings = GetCamelCaseSettings();
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;
                var normalizedData = data.Select(x =>
                    new KeyValuePair<RedisKey, RedisValue>($"{instance}:{x.Key}",
                            JsonConvert.SerializeObject(x.Value, settings))).ToArray();
                _logger.LogInformation("Insert set of items to redis cache");
                await RedisDb.StringSetAsync(normalizedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }
        }

        public async Task<bool> ClearAllCache()
        {
            try
            {
                _logger.LogInformation("FLUSH all cache");
                var result = await RedisDb.ExecuteAsync("FLUSHALL");
                if (result.Resp2Type == ResultType.Error)
                {
                    _logger.LogWarning("Failed to FLUSH all cache");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return false;
            }
        }

        public T? GetRecord<T>(string cacheKey, bool isGlobal = false)
        {
            try
            {
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;
                _logger.LogDebug("Get cache with key {Instance}:{RecordId}", instance, cacheKey);
                var result = RedisDb.StringGet($"{instance}:{cacheKey}");
                if (result.HasValue)
                {
                    return JsonConvert.DeserializeObject<T>(result.ToString());
                }

                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return default(T);
            }
        }

        public async Task<T?> GetRecordAsync<T>(string cacheKey, bool isGlobal = false)
        {
            try
            {
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;
                _logger.LogDebug("Get cache {Instance}:{RecordId}", instance, cacheKey);
                var result = await RedisDb.StringGetAsync($"{instance}:{cacheKey}").ConfigureAwait(false);

                if (result.HasValue)
                {
                    return JsonConvert.DeserializeObject<T>(result.ToString());
                }

                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return default(T);
            }
        }

        public async Task<string?> GetRecordAsync(string cacheKey, bool isGlobal = false)
        {
            try
            {
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;

                _logger.LogDebug("Get cache {Instance}:{RecordId}", instance, cacheKey);
                var result = await RedisDb.StringGetAsync($"{instance}:{cacheKey}").ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return default(string);
            }
        }

        public async Task RemoveRecordAsync(string cacheKey, bool isGlobal = false)
        {
            try
            {
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;
                _logger.LogDebug("Remove cache {Instance}:{RecordId}", instance, cacheKey);
                await RedisDb.KeyDeleteAsync($"{instance}:{cacheKey}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }
        }

        public async Task SetRecordAsync<T>(
            string cacheKey,
            T data,
            TimeSpan? absoluteExpiteTime = null,
            TimeSpan? unusedExpiteTime = null,
            bool isGlobal = false)
        {
            try
            {
                var settings = GetCamelCaseSettings();
                var expireTime = absoluteExpiteTime ?? _config.DefaultExpirationTime;
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;
                _logger.LogDebug("Set cache {Instance}:{RecordId}", instance, cacheKey);
                await RedisDb.StringSetAsync($"{instance}:{cacheKey}",
                        JsonConvert.SerializeObject(data, settings), expiry: expireTime)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }
        }

        public async IAsyncEnumerable<string> GetKeysAsync(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

            await foreach (var key in RedisServer.KeysAsync(pattern: pattern))
            {
                yield return key.ToString();
            }
        }

        public async Task<long> RemoveByControllerName(string controllerName)
        {
            if (string.IsNullOrWhiteSpace(controllerName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(controllerName));

            List<RedisKey> keys = new List<RedisKey>();
            // get all the keys* and remove each one
            await foreach (var key in GetKeysAsync("*" + controllerName + "*"))
            {
                keys.Add(key);
            }

            var numberOfDeletedKeys = await RedisDb.KeyDeleteAsync(keys.ToArray());
            _logger.LogDebug("{NumberOfDeletedKeys} has deleted from {ControllerName}", numberOfDeletedKeys,
                controllerName);

            return numberOfDeletedKeys;
        }

        public Task<bool> DeleteByRegex(string regex)
        {
            if (string.IsNullOrWhiteSpace(regex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(regex));

            // Use regular expression to match keys
            var keys = RedisServer.Keys(pattern: regex);

            // Delete all Redis keys that match the pattern
            if (keys.Any())
            {
                foreach (var key in keys)
                {
                    RedisDb.KeyDelete(key);
                }
                // Delete all Redis keys that match the pattern

                keys = RedisServer.Keys(pattern: regex);

                if (!keys.Any())
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public async Task<T?> GetValueTypeRecordAsync<T>(string cacheKey, bool isGlobal = false) where T : struct
        {
            try
            {
                string instance = isGlobal ? _config.GlobalInstance : _config.InstanceName;
                _logger.LogDebug("Get cache {Instance}:{RecordId}", instance, cacheKey);
                var result = await RedisDb.StringGetAsync($"{instance}:{cacheKey}").ConfigureAwait(false);

                if (result.HasValue)
                {
                    return JsonConvert.DeserializeObject<T>(result.ToString());
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _connectionMultiplexer?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~StackRedisCache()
        {
            Dispose(false);
        }
    }
}

