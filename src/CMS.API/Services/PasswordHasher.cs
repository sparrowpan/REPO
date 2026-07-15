using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Services;

/// <summary>
/// Hashes passwords the same way <c>AppUserRepository</c> stores them: SHA-256 of the UTF-8
/// bytes, rendered as a lowercase hex string. Kept as a shared helper so login and storage
/// stay in agreement.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    /// <summary>Constant-time comparison of the supplied password against a stored hash.</summary>
    public static bool Verify(string password, string storedHash)
    {
        var candidate = Encoding.UTF8.GetBytes(Hash(password));
        var stored = Encoding.UTF8.GetBytes(storedHash ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(candidate, stored);
    }
}
