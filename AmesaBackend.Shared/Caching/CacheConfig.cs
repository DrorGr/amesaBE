namespace AmesaBackend.Shared.Caching
{
    public class CacheConfig
    {
        public TimeSpan DefaultExpirationTime { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan DefaultShortExpirationTime { get; set; } = TimeSpan.FromMinutes(5);
        public string InstanceName { get; set; } = "AmesaBackend";
        public string GlobalInstance { get; set; } = "Global";
        public string RedisConnection { get; set; } = string.Empty;
        public bool UseHealthCheck { get; set; } = true;
    }
}

