using System.Security.Cryptography;
using System.Text;

namespace NeoAgenda.Infrastructure.Security;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifyPassword(string password, string hash)
    {
        var computed = HashPassword(password);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash)
        );
    }
}
