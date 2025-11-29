using System.Text.Json;
using System.Data;
using System.Data.Common;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Lottery.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(LotteryDbContext context, ILogger<ConfigurationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SystemConfigurationDto?> GetConfigurationAsync(string key)
        {
            try
            {
                // Access system_configurations table in amesa_auth schema using raw SQL
                // Using parameterized query to prevent SQL injection
                var sql = @"
                    SELECT id, key, value, description, is_active 
                    FROM amesa_auth.system_configurations 
                    WHERE key = {0} AND is_active = true 
                    LIMIT 1";
                
                // Use raw SQL query to access amesa_auth schema
                var connection = _context.Database.GetDbConnection();
                var wasOpen = connection.State == ConnectionState.Open;
                if (!wasOpen)
                {
                    await connection.OpenAsync();
                }
                
                try
                {
                    using var command = connection.CreateCommand();
                    // Use parameterized query for safety
                    var param = command.CreateParameter();
                    param.ParameterName = "@key";
                    param.Value = key;
                    command.Parameters.Add(param);
                    command.CommandText = @"
                        SELECT id, key, value, description, is_active 
                        FROM amesa_auth.system_configurations 
                        WHERE key = @key AND is_active = true 
                        LIMIT 1";
                    
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new SystemConfigurationDto
                        {
                            Id = reader.GetGuid(0),
                            Key = reader.GetString(1),
                            Value = reader.GetString(2),
                            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsActive = reader.GetBoolean(4)
                        };
                    }
                    
                    return null;
                }
                finally
                {
                    if (!wasOpen)
                    {
                        await connection.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration for key: {Key}", key);
                return null;
            }
        }

        public async Task<bool> IsFeatureEnabledAsync(string key)
        {
            try
            {
                var config = await GetConfigurationAsync(key);
                if (config == null)
                {
                    return false;
                }

                var valueJson = JsonDocument.Parse(config.Value);
                if (valueJson.RootElement.TryGetProperty("enabled", out var enabledElement))
                {
                    return enabledElement.GetBoolean();
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if feature is enabled for key: {Key}", key);
                return false;
            }
        }

        public async Task<T?> GetConfigurationValueAsync<T>(string key) where T : class
        {
            try
            {
                var config = await GetConfigurationAsync(key);
                if (config == null)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(config.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing configuration value for key: {Key}", key);
                return null;
            }
        }

        private class SystemConfigurationRaw
        {
            public Guid Id { get; set; }
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
        }
    }
}

