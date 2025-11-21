using System.Security.Cryptography;
using System.Text;

namespace AmesaBackend.DatabaseSeeder.Services
{
    public class PasswordHashingService
    {
        private const string Salt = "AmesaSecureSalt2024!";

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + Salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var computedHash = HashPassword(password);
            return computedHash == hashedPassword;
        }
    }
}
