using System.Text.Json;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Auth.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(AuthDbContext context, ILogger<ConfigurationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SystemConfigurationDto?> GetConfigurationAsync(string key)
        {
            try
            {
                var config = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key && c.IsActive);
                
                if (config == null)
                {
                    return null;
                }

                return new SystemConfigurationDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value,
                    Description = config.Description,
                    IsActive = config.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration for key: {Key}", key);
                throw;
            }
        }

        public async Task<SystemConfiguration> SetConfigurationAsync(string key, string value, string? description = null)
        {
            try
            {
                var existing = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key);

                if (existing != null)
                {
                    existing.Value = value;
                    existing.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(description))
                    {
                        existing.Description = description;
                    }
                }
                else
                {
                    existing = new SystemConfiguration
                    {
                        Key = key,
                        Value = value,
                        Description = description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.SystemConfigurations.Add(existing);
                }

                await _context.SaveChangesAsync();
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting configuration for key: {Key}", key);
                throw;
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

    }
}

