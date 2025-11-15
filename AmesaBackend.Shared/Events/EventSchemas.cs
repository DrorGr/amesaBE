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
        public Guid TicketId { get; set; }
        public Guid HouseId { get; set; }
        public Guid UserId { get; set; }
        public int TicketNumber { get; set; }
        public decimal Price { get; set; }
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
}

