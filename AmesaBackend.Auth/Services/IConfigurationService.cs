using AmesaBackend.Shared.Configuration;
using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.Services
{
    public interface IConfigurationService : AmesaBackend.Shared.Configuration.IConfigurationService
    {
        Task<SystemConfiguration> SetConfigurationAsync(string key, string value, string? description = null);
    }
}

