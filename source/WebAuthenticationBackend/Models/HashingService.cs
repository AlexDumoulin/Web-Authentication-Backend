using System.Security.Cryptography;
using System.Text;

namespace WebAuthenticationBackend.Models
{
    public class HashingService
    {
        public static string GenerateSalt()
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            return Convert.ToBase64String(salt);
        }

        public static string ComputeHash(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(combined);
            return Convert.ToHexString(hash);
        }
    }
}
