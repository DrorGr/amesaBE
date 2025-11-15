namespace AmesaBackend.Shared.Enums
{
    // User Enums
    public enum UserStatus
    {
        Active,
        Inactive,
        Pending,
        Suspended,
        Banned
    }

    public enum UserVerificationStatus
    {
        Unverified,
        EmailVerified,
        PhoneVerified,
        FullyVerified
    }

    public enum AuthProvider
    {
        Email,
        Google,
        Facebook,
        Apple
    }

    public enum GenderType
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }

    // Lottery Enums
    public enum LotteryStatus
    {
        Upcoming,
        Active,
        Ended,
        Cancelled,
        Completed
    }

    public enum TicketStatus
    {
        Active,
        Cancelled,
        Refunded,
        Expired
    }

    public enum DrawStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled,
        Failed
    }

    // Payment Enums
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        PayPal,
        BankTransfer,
        Crypto
    }

    public enum TransactionType
    {
        TicketPurchase,
        Refund,
        Withdrawal,
        Deposit,
        Fee
    }

    // Media Enums
    public enum MediaType
    {
        Image,
        Video,
        Document,
        Audio
    }

    // Content Enums
    public enum ContentStatus
    {
        Draft,
        Published,
        Archived
    }

    // Lottery Results Enums
    public enum ClaimStatus
    {
        Pending,
        Claimed,
        Delivered,
        Expired,
        Cancelled
    }

    public enum DeliveryStatus
    {
        Pending,
        Scheduled,
        InTransit,
        Delivered,
        Failed,
        Returned
    }
}

