using System;
using System.Security.Cryptography;

namespace EmployeeTimeClockApp.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;    // 128-bit
        private const int KeySize = 32;    // 256-bit
        private const int Iterations = 100_000;

        public static (string HashB64, string SaltB64, int Iterations) HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));

            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            byte[] key;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                key = pbkdf2.GetBytes(KeySize);

            return (Convert.ToBase64String(key), Convert.ToBase64String(salt), Iterations);
        }
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }


        public static bool VerifyPassword(string password, string hashB64, string saltB64, int iterations)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(hashB64) ||
                string.IsNullOrWhiteSpace(saltB64) ||
                iterations <= 0)
                return false;

            byte[] salt = Convert.FromBase64String(saltB64);
            byte[] expected = Convert.FromBase64String(hashB64);

            byte[] actual;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                actual = pbkdf2.GetBytes(expected.Length);

            return FixedTimeEquals(actual, expected);
        }
    }
}
