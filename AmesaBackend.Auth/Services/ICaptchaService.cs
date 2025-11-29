namespace AmesaBackend.Auth.Services
{
    public interface ICaptchaService
    {
        Task<bool> VerifyCaptchaAsync(string token, string? action = null);
    }
}

