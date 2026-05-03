using System.Security.Cryptography;
using System.Text;

namespace ProiectBanking.Helpers
{
    public static class SecurityHelper
    {
        // Transformă parola într-un șir ilizibil de caractere
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}