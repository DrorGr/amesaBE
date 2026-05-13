namespace AmesaBackend.Admin.Models;

public sealed class AdminSignInResult
{
    public bool Succeeded { get; init; }
    public bool RequiresMfa { get; init; }
    public string? ErrorMessage { get; init; }

    public static AdminSignInResult Success() => new() { Succeeded = true };
    public static AdminSignInResult MfaRequired() => new() { RequiresMfa = true };
    public static AdminSignInResult Failed(string? message = null) => new() { ErrorMessage = message };
}
