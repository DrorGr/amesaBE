namespace AmesaBackend.Shared.Events
{
    /// <summary>
    /// Base event class for all domain events
    /// </summary>
    public abstract class DomainEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
        public string DetailType { get; set; } = string.Empty;
    }

    // User Events
    public class UserCreatedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class EmailVerificationRequestedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string VerificationToken { get; set; } = string.Empty;
    }

    public class PasswordResetRequestedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
    }

    public class UserEmailVerifiedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class UserUpdatedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class ProfileUpdatedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<string> ChangedFields { get; set; } = new(); // ["firstName", "lastName", "phone", etc.]
    }

    public class PreferencesUpdatedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string PreferenceCategory { get; set; } = string.Empty; // "notifications", "privacy", "lottery", etc.
        public Dictionary<string, object> ChangedPreferences { get; set; } = new();
    }

    public class SystemAnnouncementEvent : DomainEvent
    {
        public Guid AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "info"; // info, warning, error
        public List<Guid>? TargetUserIds { get; set; } // null = all users
        public string? TargetUserSegment { get; set; } // "all", "active", "premium", etc.
        public DateTime? ExpiresAt { get; set; }
    }

    public class UserVerifiedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string VerificationType { get; set; } = string.Empty; // Email or Phone
    }

    public class UserLoginEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class AccountLockedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string LockReason { get; set; } = string.Empty;
        public DateTime? LockedUntil { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    public class AccountUnlockedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UnlockReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event emitted when repeated failed login attempts are detected for a user.
    /// </summary>
    public class FailedLoginAttemptsEvent : DomainEvent
    {
        /// <summary>
        /// The identifier of the affected user.
        /// </summary>
        public Guid UserId { get; set; }

        public string Email { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastAttemptAt { get; set; }
    }

    public class NewDeviceLoginEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string? Location { get; set; }
    }

    public class NewLocationLoginEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? City { get; set; }
    }

    public class TwoFactorEnabledEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string TwoFactorMethod { get; set; } = string.Empty; // SMS, Authenticator, Email
    }

    public class TwoFactorDisabledEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisabledReason { get; set; } = string.Empty;
    }

    public class PasswordChangedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool ChangedByUser { get; set; } = true;
        public Guid? ChangedByAdminId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    public class EmailChangedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string OldEmail { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class PhoneVerifiedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class SuspiciousActivityEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // "unusual_login", "multiple_failed_attempts", etc.
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Severity { get; set; } = "medium"; // low, medium, high, critical
    }

    // House Events
    public class HouseCreatedEvent : DomainEvent
    {
        public Guid HouseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid CreatedByUserId { get; set; }
    }

    public class HouseUpdatedEvent : DomainEvent
    {
        public Guid HouseId { get; set; }
        public string? Title { get; set; }
        public decimal? Price { get; set; }
    }

    // Ticket Events
    public class TicketPurchasedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public Guid HouseId { get; set; }
        public int TicketCount { get; set; }
        public List<string> TicketNumbers { get; set; } = new();
    }

    public class TicketRefundedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public Guid HouseId { get; set; }
        public Guid TicketId { get; set; }
        public int TicketNumber { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundReason { get; set; } = string.Empty;
    }

    // Lottery Draw Events
    public class LotteryDrawStartingEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public DateTime DrawStartTime { get; set; }
        public int MinutesUntilStart { get; set; }
    }

    public class LotteryDrawStartedEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
    }

    public class LotteryDrawCompletedEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public DateTime DrawDate { get; set; }
        public int TotalTickets { get; set; }
    }

    public class LotteryDrawFailedEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }

    public class LotteryEndedEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
        public DateTime EndedAt { get; set; }
        public Guid? WinnerUserId { get; set; }
        public string? WinnerName { get; set; }
        public bool WasCancelled { get; set; } = false;
        public string? CancellationReason { get; set; }
    }

    public class FavoriteAddedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
    }

    public class FavoriteRemovedEvent : DomainEvent
    {
        public Guid UserId { get; set; }
        public Guid HouseId { get; set; }
        public string HouseTitle { get; set; } = string.Empty;
    }

    public class LotteryDrawWinnerSelectedEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public Guid WinnerTicketId { get; set; }
        public Guid WinnerUserId { get; set; }
        public int WinningTicketNumber { get; set; }
        
        // Prize information (optional - included when available)
        public string? HouseTitle { get; set; }
        public decimal? PrizeValue { get; set; }
        public string? PrizeDescription { get; set; }
    }

    // Payment Events
    public class PaymentInitiatedEvent : DomainEvent
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class PaymentCompletedEvent : DomainEvent
    {
        public Guid PaymentId { get; set; }
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class PaymentFailedEvent : DomainEvent
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }

    public class PaymentRefundedEvent : DomainEvent
    {
        public Guid PaymentId { get; set; }
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundReason { get; set; } = string.Empty;
    }

    public class PaymentDisputedEvent : DomainEvent
    {
        public Guid PaymentId { get; set; }
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string DisputeReason { get; set; } = string.Empty;
        public string DisputeType { get; set; } = string.Empty; // chargeback, refund_request, etc.
        public string Status { get; set; } = "pending"; // pending, under_review, won, lost
    }

    // Lottery Result Events
    public class LotteryResultCreatedEvent : DomainEvent
    {
        public Guid ResultId { get; set; }
        public Guid DrawId { get; set; }
        public Guid WinnerUserId { get; set; }
        public Guid WinnerTicketId { get; set; }
    }

    public class PrizeClaimedEvent : DomainEvent
    {
        public Guid ResultId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ClaimedAt { get; set; }
    }

    public class PrizeDeliveredEvent : DomainEvent
    {
        public Guid ResultId { get; set; }
        public Guid UserId { get; set; }
        public DateTime DeliveredAt { get; set; }
    }

    // Content Events
    public class TranslationUpdatedEvent : DomainEvent
    {
        public string Key { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ContentPublishedEvent : DomainEvent
    {
        public Guid ContentId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    // Promotion Events
    public class PromotionCreatedEvent : DomainEvent
    {
        public Guid PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }

    // System Events
    public class SystemSettingUpdatedEvent : DomainEvent
    {
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
    }

    // Notification Events
    public class NotificationSentEvent : DomainEvent
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class NotificationDeliveredEvent : DomainEvent
    {
        public Guid DeliveryId { get; set; }
        public Guid NotificationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
    }

    public class NotificationFailedEvent : DomainEvent
    {
        public Guid DeliveryId { get; set; }
        public Guid NotificationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
    }

    public class NotificationBouncedEvent : DomainEvent
    {
        public Guid DeliveryId { get; set; }
        public Guid NotificationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string BounceType { get; set; } = string.Empty;
        public string BounceReason { get; set; } = string.Empty;
    }

    public class NotificationOpenedEvent : DomainEvent
    {
        public Guid DeliveryId { get; set; }
        public Guid NotificationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
    }

    public class NotificationClickedEvent : DomainEvent
    {
        public Guid DeliveryId { get; set; }
        public Guid NotificationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public DateTime ClickedAt { get; set; }
        public string? ClickUrl { get; set; }
    }
}

