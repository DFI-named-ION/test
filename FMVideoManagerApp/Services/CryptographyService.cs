using System.IO;
using System.Security.Cryptography;
using System.Security.Policy;

namespace FMVideoManagerApp.Services
{
    internal static class CryptographyService
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPasswordHash(string inputPassword, string dbPassword)
        {
            string[] parts = dbPassword.Split('.');

            if (parts.Length != 3)
                return false;

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] storedHash = Convert.FromBase64String(parts[2]);

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
                inputPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                storedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(storedHash, inputHash);
        }

        public static string HashFile(FileInfo file)
        {
            var hash = string.Empty;

            using (var fs = file.OpenRead())
            {
                var sha = SHA256.Create();
                var byteHash = sha.ComputeHash(fs);
                hash = BitConverter.ToString(byteHash).Replace("-", "").ToLowerInvariant();
            }
            return hash;
        }
    }
}