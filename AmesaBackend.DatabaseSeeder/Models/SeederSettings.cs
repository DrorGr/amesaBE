namespace AmesaBackend.DatabaseSeeder.Models
{
    public class SeederSettings
    {
        public string Environment { get; set; } = "Development";
        public bool TruncateExistingData { get; set; } = true;
        public bool SeedLanguages { get; set; } = true;
        public bool SeedTranslations { get; set; } = true;
        public bool SeedUsers { get; set; } = true;
        public bool SeedHouses { get; set; } = true;
        public int BatchSize { get; set; } = 100;
        public bool EnableDetailedLogging { get; set; } = true;
    }
}
