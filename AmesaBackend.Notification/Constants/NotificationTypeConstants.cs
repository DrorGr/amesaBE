namespace AmesaBackend.Notification.Constants
{
    /// <summary>
    /// Standardized notification type codes
    /// </summary>
    public static class NotificationTypeConstants
    {
        // Authentication
        public const string UserRegistered = "user_registered";
        public const string EmailVerificationRequested = "email_verification_requested";
        public const string EmailVerified = "email_verified";
        public const string PasswordResetRequested = "password_reset_requested";
        public const string PasswordChanged = "password_changed";

        // Security
        public const string AccountLocked = "account_locked";
        public const string AccountUnlocked = "account_unlocked";
        public const string FailedLoginAttempts = "failed_login_attempts";
        public const string NewDeviceLogin = "new_device_login";
        public const string NewLocationLogin = "new_location_login";
        public const string TwoFactorEnabled = "two_factor_enabled";
        public const string TwoFactorDisabled = "two_factor_disabled";
        public const string SuspiciousActivity = "suspicious_activity";
        public const string EmailChanged = "email_changed";
        public const string PhoneVerified = "phone_verified";

        // Lottery
        public const string TicketPurchased = "ticket_purchased";
        public const string TicketRefunded = "ticket_refunded";
        public const string LotteryDrawStarting = "lottery_draw_starting";
        public const string LotteryDrawStarted = "lottery_draw_started";
        public const string LotteryDrawCompleted = "lottery_draw_completed";
        public const string LotteryWinnerSelected = "lottery_winner_selected";
        public const string LotteryEnded = "lottery_ended";
        public const string HouseCreated = "house_created";
        public const string HouseUpdated = "house_updated";
        public const string FavoriteAdded = "favorite_added";
        public const string FavoriteRemoved = "favorite_removed";

        // Payment
        public const string PaymentInitiated = "payment_initiated";
        public const string PaymentCompleted = "payment_completed";
        public const string PaymentFailed = "payment_failed";
        public const string PaymentRefunded = "payment_refunded";
        public const string PaymentDisputed = "payment_disputed";

        // Profile
        public const string ProfileUpdated = "profile_updated";
        public const string PreferencesUpdated = "preferences_updated";

        // System
        public const string SystemAnnouncement = "system_announcement";
        public const string PromotionCreated = "promotion_created";
        public const string ContentPublished = "content_published";
    }

    /// <summary>
    /// Notification feature categories
    /// </summary>
    public static class NotificationFeatureConstants
    {
        public const string Authentication = "authentication";
        public const string Security = "security";
        public const string Lottery = "lottery";
        public const string Payment = "payment";
        public const string Profile = "profile";
        public const string System = "system";
    }

    /// <summary>
    /// Notification categories
    /// </summary>
    public static class NotificationCategoryConstants
    {
        public const string Authentication = "authentication";
        public const string Security = "security";
        public const string Lottery = "lottery";
        public const string Payment = "payment";
        public const string Profile = "profile";
        public const string System = "system";
    }
}

