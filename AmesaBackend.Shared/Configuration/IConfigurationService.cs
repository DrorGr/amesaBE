namespace AmesaBackend.Shared.Configuration
{
    public interface IConfigurationService
    {
        Task<SystemConfigurationDto?> GetConfigurationAsync(string key);
        Task<bool> IsFeatureEnabledAsync(string key);
        Task<T?> GetConfigurationValueAsync<T>(string key) where T : class;
    }

    public class SystemConfigurationDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}

