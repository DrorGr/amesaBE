using System.Security.Claims;

namespace AmesaBackend.Shared.Authentication
{
    public interface IJwtTokenManager
    {
        string GenerateAccessToken(IEnumerable<Claim> authClaims, DateTime tokenExpiration);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        string GenerateRefreshToken();
    }
}

