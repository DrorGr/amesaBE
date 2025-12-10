using StackExchange.Redis;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Lottery.Services
{
    public class RedisInventoryManager : IRedisInventoryManager
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly LotteryDbContext _context;
        private readonly ILogger<RedisInventoryManager> _logger;
        private readonly IDatabase _db;

        public RedisInventoryManager(
            IConnectionMultiplexer redis,
            LotteryDbContext context,
            ILogger<RedisInventoryManager> logger)
        {
            _redis = redis;
            _context = context;
            _logger = logger;
            _db = redis.GetDatabase();
        }

        public async Task<bool> ReserveInventoryAsync(Guid houseId, int quantity, string reservationToken)
        {
            try
            {
                // Lua script for atomic reserve operation
                var script = @"
                    local houseKey = 'lottery:inventory:' .. ARGV[1]
                    local reservedKey = 'lottery:inventory:' .. ARGV[1] .. ':reserved'
                    local lockKey = 'lottery:inventory:' .. ARGV[1] .. ':lock:' .. ARGV[3]
                    local quantity = tonumber(ARGV[2])
                    local token = ARGV[3]
                    
                    local available = tonumber(redis.call('GET', houseKey)) or 0
                    
                    if available < quantity then
                        return 0
                    end
                    
                    -- Reserve inventory
                    redis.call('DECRBY', houseKey, quantity)
                    redis.call('INCRBY', reservedKey, quantity)
                    redis.call('SET', lockKey, '1', 'EX', 300)
                    
                    return 1
                ";

                var result = await _db.ScriptEvaluateAsync(
                    script,
                    keys: Array.Empty<RedisKey>(),
                    values: new RedisValue[] { houseId.ToString(), quantity, reservationToken });

                return (int)result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving inventory for house {HouseId}", houseId);
                return false;
            }
        }

        public async Task<bool> ReleaseInventoryAsync(Guid houseId, int quantity)
        {
            try
            {
                var script = @"
                    local houseKey = 'lottery:inventory:' .. ARGV[1]
                    local reservedKey = 'lottery:inventory:' .. ARGV[1] .. ':reserved'
                    local quantity = tonumber(ARGV[2])
                    
                    local reserved = tonumber(redis.call('GET', reservedKey)) or 0
                    
                    if reserved < quantity then
                        quantity = reserved
                    end
                    
                    redis.call('INCRBY', houseKey, quantity)
                    redis.call('DECRBY', reservedKey, quantity)
                    
                    return 1
                ";

                await _db.ScriptEvaluateAsync(
                    script,
                    keys: Array.Empty<RedisKey>(),
                    values: new RedisValue[] { houseId.ToString(), quantity });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing inventory for house {HouseId}", houseId);
                return false;
            }
        }

        public async Task<InventoryStatus> GetInventoryStatusAsync(Guid houseId)
        {
            try
            {
                // Try Redis first
                var houseKey = $"lottery:inventory:{houseId}";
                var reservedKey = $"lottery:inventory:{houseId}:reserved";
                var soldKey = $"lottery:inventory:{houseId}:sold";

                var available = await _db.StringGetAsync(houseKey);
                var reserved = await _db.StringGetAsync(reservedKey);
                var sold = await _db.StringGetAsync(soldKey);

                var house = await _context.Houses.FindAsync(houseId);
                if (house == null)
                {
                    throw new InvalidOperationException($"House {houseId} not found");
                }

                var totalTickets = house.TotalTickets;
                var availableCount = available.HasValue ? (int)available : await GetAvailableFromDatabaseAsync(houseId);
                var reservedCount = reserved.HasValue ? (int)reserved : 0;
                var soldCount = sold.HasValue ? (int)sold : await GetSoldFromDatabaseAsync(houseId);

                var timeRemaining = house.LotteryEndDate - DateTime.UtcNow;
                var isEnded = timeRemaining <= TimeSpan.Zero;
                var isSoldOut = availableCount <= 0 && reservedCount == 0;

                return new InventoryStatus
                {
                    HouseId = houseId,
                    TotalTickets = totalTickets,
                    AvailableTickets = Math.Max(0, availableCount),
                    ReservedTickets = reservedCount,
                    SoldTickets = soldCount,
                    LotteryEndDate = house.LotteryEndDate,
                    TimeRemaining = isEnded ? TimeSpan.Zero : timeRemaining,
                    IsSoldOut = isSoldOut,
                    IsEnded = isEnded
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory status for house {HouseId}", houseId);
                // Fallback to database
                return await GetInventoryStatusFromDatabaseAsync(houseId);
            }
        }

        public async Task<int> GetAvailableCountAsync(Guid houseId)
        {
            try
            {
                var houseKey = $"lottery:inventory:{houseId}";
                var result = await _db.StringGetAsync(houseKey);
                
                if (result.HasValue)
                {
                    return (int)result;
                }

                // Fallback to database
                return await GetAvailableFromDatabaseAsync(houseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available count for house {HouseId}", houseId);
                return await GetAvailableFromDatabaseAsync(houseId);
            }
        }

        public async Task<bool> CheckParticipantCapAsync(Guid houseId, Guid userId)
        {
            try
            {
                var house = await _context.Houses.FindAsync(houseId);
                if (house?.MaxParticipants == null)
                {
                    return true; // No cap
                }

                var participantsKey = $"lottery:participants:{houseId}:users";
                var isMember = await _db.SetContainsAsync(participantsKey, userId.ToString());

                if (isMember)
                {
                    return true; // User is already a participant
                }

                var countKey = $"lottery:participants:{houseId}:count";
                var count = await _db.StringGetAsync(countKey);
                var currentCount = count.HasValue ? (int)count : await GetParticipantCountFromDatabaseAsync(houseId);

                return currentCount < house.MaxParticipants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking participant cap for house {HouseId}", houseId);
                return true; // Fail open
            }
        }

        public async Task<bool> AddParticipantAsync(Guid houseId, Guid userId)
        {
            try
            {
                var script = @"
                    local participantsKey = 'lottery:participants:' .. ARGV[1] .. ':users'
                    local countKey = 'lottery:participants:' .. ARGV[1] .. ':count'
                    local userId = ARGV[2]
                    local maxParticipants = tonumber(ARGV[3])
                    
                    if maxParticipants == 0 then
                        -- No cap
                        redis.call('SADD', participantsKey, userId)
                        redis.call('INCR', countKey)
                        return 1
                    end
                    
                    local currentCount = tonumber(redis.call('GET', countKey)) or 0
                    
                    if currentCount >= maxParticipants then
                        return 0
                    end
                    
                    local isMember = redis.call('SISMEMBER', participantsKey, userId)
                    if isMember == 1 then
                        return 1
                    end
                    
                    redis.call('SADD', participantsKey, userId)
                    redis.call('INCR', countKey)
                    
                    return 1
                ";

                var house = await _context.Houses.FindAsync(houseId);
                var maxParticipants = house?.MaxParticipants ?? 0;

                var result = await _db.ScriptEvaluateAsync(
                    script,
                    keys: Array.Empty<RedisKey>(),
                    values: new RedisValue[] { houseId.ToString(), userId.ToString(), maxParticipants });

                return (int)result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding participant for house {HouseId}", houseId);
                return false;
            }
        }

        private async Task<int> GetAvailableFromDatabaseAsync(Guid houseId)
        {
            var house = await _context.Houses.FindAsync(houseId);
            if (house == null) return 0;

            var soldCount = await _context.LotteryTickets
                .Where(t => t.HouseId == houseId && t.Status == "Active")
                .CountAsync();

            return Math.Max(0, house.TotalTickets - soldCount);
        }

        private async Task<int> GetSoldFromDatabaseAsync(Guid houseId)
        {
            return await _context.LotteryTickets
                .Where(t => t.HouseId == houseId && t.Status == "Active")
                .CountAsync();
        }

        private async Task<int> GetParticipantCountFromDatabaseAsync(Guid houseId)
        {
            return await _context.LotteryTickets
                .Where(t => t.HouseId == houseId && t.Status == "Active")
                .Select(t => t.UserId)
                .Distinct()
                .CountAsync();
        }

        private async Task<InventoryStatus> GetInventoryStatusFromDatabaseAsync(Guid houseId)
        {
            var house = await _context.Houses.FindAsync(houseId);
            if (house == null)
            {
                throw new InvalidOperationException($"House {houseId} not found");
            }

            var soldCount = await GetSoldFromDatabaseAsync(houseId);
            var timeRemaining = house.LotteryEndDate - DateTime.UtcNow;
            var isEnded = timeRemaining <= TimeSpan.Zero;

            return new InventoryStatus
            {
                HouseId = houseId,
                TotalTickets = house.TotalTickets,
                AvailableTickets = Math.Max(0, house.TotalTickets - soldCount),
                ReservedTickets = 0,
                SoldTickets = soldCount,
                LotteryEndDate = house.LotteryEndDate,
                TimeRemaining = isEnded ? TimeSpan.Zero : timeRemaining,
                IsSoldOut = house.TotalTickets <= soldCount,
                IsEnded = isEnded
            };
        }
    }
}












