namespace AmesaBackend.Lottery.Configuration
{
    public class LotterySettings
    {
        public ReservationSettings Reservation { get; set; } = new();
        public BackgroundServiceSettings BackgroundServices { get; set; } = new();
        public PaymentSettings Payment { get; set; } = new();
    }

    public class ReservationSettings
    {
        public int ExpiryMinutes { get; set; } = 5;
        public RateLimitSettings RateLimit { get; set; } = new();
    }

    public class RateLimitSettings
    {
        public int PerUser { get; set; } = 5;
        public int PerUserHouse { get; set; } = 10;
        public int WindowHours { get; set; } = 1;
    }

    public class BackgroundServiceSettings
    {
        public int InventorySyncIntervalMinutes { get; set; } = 5;
        public int ReservationCleanupIntervalMinutes { get; set; } = 1;
        public int LotteryDrawCheckIntervalMinutes { get; set; } = 5;
    }

    public class PaymentSettings
    {
        public int TimeoutSeconds { get; set; } = 30;
    }
}

