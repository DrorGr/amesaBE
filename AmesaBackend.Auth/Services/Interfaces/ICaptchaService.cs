namespace AmesaBackend.Auth.Services.Interfaces
{
    public interface ICaptchaService
    {
        Task<bool> VerifyCaptchaAsync(string token, string? action = null);
    }
}

