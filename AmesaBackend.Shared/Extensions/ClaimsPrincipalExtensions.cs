using System.Security.Claims;
using AmesaBackend.Shared.Enums;

namespace AmesaBackend.Shared.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Return provided claim value else null.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <param name="name"></param>
        /// <returns>Returns string value if claim exists else null.</returns>
        public static string? GetClaimValue(this ClaimsPrincipal claimsPrincipal, string name)
        {
            var claim = claimsPrincipal.FindFirst(name);
            return claim?.Value;
        }

        /// <summary>
        /// Checks if provided claims are present in ClaimsPrincipal's claims.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <param name="claims"></param>
        /// <returns>Returns true if every single of provided claims are found.</returns>
        public static bool HasClaims(this ClaimsPrincipal claimsPrincipal, params string[] claims)
        {
            foreach (var claim in claims)
            {
                if (claimsPrincipal.Claims.All(c => c.Type != claim))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to find ClaimTypes.NameIdentifier in ClaimPrincipal's claims.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <returns>Returns <b>user id</b> from claims or throws exception if user id claims was not found.</returns>
        /// <exception cref="ApplicationException"></exception>
        public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null || string.IsNullOrEmpty(userId.Value))
            {
                throw new ApplicationException("User id claim is not found!");
            }

            if (!Guid.TryParse(userId.Value, out var parsedUserId))
            {
                throw new ApplicationException("User id claim contains invalid GUID format!");
            }

            return parsedUserId;
        }

        /// <summary>
        /// Tries to find ClaimTypes.Name in ClaimPrincipal's claims.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <returns>Returns username from claims or empty string if <b>username</b> was not found.</returns>
        public static string GetUserName(this ClaimsPrincipal claimsPrincipal)
        {
            var userName = claimsPrincipal.FindFirst(ClaimTypes.Name);
            return userName?.Value ?? string.Empty;
        }

        public static int GetLanguageId(this ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                var languageId = GetClaimValue(claimsPrincipal, AMESAClaimTypes.LanguageId.ToString());
                return string.IsNullOrEmpty(languageId) ? 1 : int.Parse(languageId);
            }
            catch
            {
                return 1;
            }
        }
    }
}

