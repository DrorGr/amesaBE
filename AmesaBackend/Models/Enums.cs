using NpgsqlTypes;

namespace AmesaBackend.Models
{
    public enum UserStatus
    {
        Pending,
        Active,
        Suspended,
        Banned,
        Deleted
    }

    public enum UserVerificationStatus
    {
        Unverified,
        EmailVerified,
        PhoneVerified,
        IdentityVerified,
        FullyVerified
    }

    public enum AuthProvider
    {
        Email,
        Google,
        Meta,
        Apple,
        Twitter
    }

    public enum GenderType
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }

    public enum LotteryStatus
    {
        Upcoming,
        Active,
        Paused,
        Ended,
        Cancelled,
        Completed
    }

    public enum TicketStatus
    {
        Active,
        Winner,
        Refunded,
        Cancelled
    }

    public enum DrawStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Refunded,
        Cancelled
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        PayPal,
        ApplePay,
        GooglePay,
        BankTransfer,
        Crypto
    }

    public enum TransactionType
    {
        TicketPurchase,
        Refund,
        Withdrawal,
        Bonus,
        Fee
    }

    public enum MediaType
    {
        Image,
        Video,
        Document,
        Audio
    }

    public enum ContentStatus
    {
        Draft,
        Published,
        Archived,
        Deleted
    }
}
