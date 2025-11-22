namespace AmesaBackend.Auth.DTOs
{
    /// <summary>
    /// Lottery-specific preferences DTO
    /// </summary>
    public class LotteryPreferencesDto
    {
        public List<string> FavoriteCategories { get; set; } = new();
        public decimal? PriceRangeMin { get; set; }
        public decimal? PriceRangeMax { get; set; }
        public List<string> PreferredLocations { get; set; } = new();
        public List<string> HouseTypes { get; set; } = new();
        public string DefaultView { get; set; } = "grid";
        public int ItemsPerPage { get; set; } = 25;
        public string SortBy { get; set; } = "price";
        public string SortOrder { get; set; } = "asc";
        public bool PriceDropAlerts { get; set; } = false;
        public bool NewMatchingLotteries { get; set; } = true;
        public bool EndingSoonAlerts { get; set; } = false;
        public bool WinnerAnnouncements { get; set; } = true;
        public List<Guid> FavoriteHouseIds { get; set; } = new();
        
        /// <summary>
        /// Notification settings for lottery preferences
        /// </summary>
        public LotteryNotificationSettings NotificationSettings { get; set; } = new();
        
        /// <summary>
        /// Quick entry settings
        /// </summary>
        public QuickEntrySettings QuickEntrySettings { get; set; } = new();
    }

    /// <summary>
    /// Notification settings for lottery preferences
    /// </summary>
    public class LotteryNotificationSettings
    {
        public bool NewLotteries { get; set; } = true;
        public bool FavoriteUpdates { get; set; } = true;
        public bool DrawReminders { get; set; } = false;
        public bool WinnerAnnouncements { get; set; } = true;
        public bool PriceDropAlerts { get; set; } = false;
        public bool EndingSoonAlerts { get; set; } = false;
    }

    /// <summary>
    /// Quick entry settings for lottery
    /// </summary>
    public class QuickEntrySettings
    {
        public int DefaultTicketCount { get; set; } = 1;
        public bool AutoEnterFavorites { get; set; } = false;
        public decimal? MaxSpendingLimit { get; set; }
        public Guid? DefaultPaymentMethodId { get; set; }
    }
}
