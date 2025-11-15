namespace AmesaBackend.Shared.Events
{
    /// <summary>
    /// Constants for EventBridge event sources and detail types
    /// </summary>
    public static class EventBridgeConstants
    {
        public const string Source = "amesa.backend";
        
        public static class DetailType
        {
            // User Events
            public const string UserCreated = "UserCreated";
            public const string UserUpdated = "UserUpdated";
            public const string UserVerified = "UserVerified";
            public const string UserLogin = "UserLogin";
            public const string EmailVerificationRequested = "EmailVerificationRequested";
            public const string PasswordResetRequested = "PasswordResetRequested";
            public const string UserEmailVerified = "UserEmailVerified";

            // House Events
            public const string HouseCreated = "HouseCreated";
            public const string HouseUpdated = "HouseUpdated";

            // Ticket Events
            public const string TicketPurchased = "TicketPurchased";

            // Lottery Draw Events
            public const string LotteryDrawCompleted = "LotteryDrawCompleted";
            public const string LotteryDrawWinnerSelected = "LotteryDrawWinnerSelected";

            // Payment Events
            public const string PaymentInitiated = "PaymentInitiated";
            public const string PaymentCompleted = "PaymentCompleted";
            public const string PaymentFailed = "PaymentFailed";
            public const string PaymentRefunded = "PaymentRefunded";

            // Lottery Result Events
            public const string LotteryResultCreated = "LotteryResultCreated";
            public const string PrizeClaimed = "PrizeClaimed";
            public const string PrizeDelivered = "PrizeDelivered";

            // Content Events
            public const string TranslationUpdated = "TranslationUpdated";
            public const string ContentPublished = "ContentPublished";

            // Promotion Events
            public const string PromotionCreated = "PromotionCreated";

            // System Events
            public const string SystemSettingUpdated = "SystemSettingUpdated";
        }
    }
}

