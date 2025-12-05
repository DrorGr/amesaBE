namespace AmesaBackend.Notification.Constants
{
    /// <summary>
    /// Constants for notification channel names and configuration
    /// </summary>
    public static class NotificationChannelConstants
    {
        // Channel Names
        public const string Email = "email";
        public const string SMS = "sms";
        public const string Push = "push";
        public const string WebPush = "webpush";
        public const string Telegram = "telegram";
        public const string SocialMedia = "socialmedia";

        // Channel Display Names
        public const string EmailDisplayName = "Email";
        public const string SMSDisplayName = "SMS";
        public const string PushDisplayName = "Push Notifications";
        public const string WebPushDisplayName = "Web Push";
        public const string TelegramDisplayName = "Telegram";
        public const string SocialMediaDisplayName = "Social Media";

        // Delivery Status
        public const string StatusPending = "pending";
        public const string StatusSent = "sent";
        public const string StatusDelivered = "delivered";
        public const string StatusFailed = "failed";
        public const string StatusBounced = "bounced";
        public const string StatusOpened = "opened";
        public const string StatusClicked = "clicked";

        // Queue Status
        public const string QueueStatusPending = "pending";
        public const string QueueStatusProcessing = "processing";
        public const string QueueStatusCompleted = "completed";
        public const string QueueStatusFailed = "failed";

        // Rate Limit Keys Prefix
        public const string RateLimitPrefix = "notification:";

        // Cache Keys Prefixes
        public const string CacheKeyUserPrefs = "user_prefs:";
        public const string CacheKeyTemplate = "template:";
        public const string CacheKeyDelivery = "delivery:";
        public const string CacheKeyRateLimit = "ratelimit:";

        // Default Values
        public const string DefaultLanguage = "en";
        public const string DefaultChannel = "all";
        public const string DefaultCurrency = "USD";
        public const int DefaultMaxRetries = 3;
        public const int DefaultQueuePriority = 5;
    }
}








