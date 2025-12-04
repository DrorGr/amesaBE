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

    // Lottery Draw Events
    public class LotteryDrawCompletedEvent : DomainEvent
    {
        public Guid DrawId { get; set; }
        public Guid HouseId { get; set; }
        public DateTime DrawDate { get; set; }
        public int TotalTickets { get; set; }
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

