using System.Security.Cryptography;

namespace TauschboerseBKH.Services;

/// <summary>
/// Passwörter werden ausschließlich als gesalzener Hashwert (PBKDF2-SHA256) gespeichert.
/// Format: Iterationen.Salt(Base64).Hash(Base64)
/// </summary>
internal static class PasswortHasher
{
    private const int Iterationen = 100_000;
    private const int SaltLaenge = 16;
    private const int HashLaenge = 32;

    public static string Hashen(string passwort)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLaenge);
        var hash = Rfc2898DeriveBytes.Pbkdf2(passwort, salt, Iterationen, HashAlgorithmName.SHA256, HashLaenge);
        return $"{Iterationen}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Pruefen(string passwort, string gespeichert)
    {
        var teile = gespeichert.Split('.');
        if (teile.Length != 3 || !int.TryParse(teile[0], out var iterationen) || iterationen < 1)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(teile[1]);
            var erwartet = Convert.FromBase64String(teile[2]);
            var hash = Rfc2898DeriveBytes.Pbkdf2(passwort, salt, iterationen, HashAlgorithmName.SHA256, erwartet.Length);
            return CryptographicOperations.FixedTimeEquals(hash, erwartet);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
