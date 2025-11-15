namespace AmesaBackend.Auth.Models
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
}

