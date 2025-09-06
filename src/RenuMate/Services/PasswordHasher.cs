using System.Security.Cryptography;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; 
    private const int KeySize = 32;  
    private const int Iterations = 100_000; 

    public string HashPassword(string raw)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(raw, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        var hash = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        
        return hash;
    }

    public bool VerifyHashedPassword(string raw, string hashed)
    {
        var parts = hashed.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var key = Convert.FromBase64String(parts[1]);

        using var pbkdf2 = new Rfc2898DeriveBytes(raw, salt, Iterations, HashAlgorithmName.SHA256);
        var keyToCheck = pbkdf2.GetBytes(KeySize);

        var verified = CryptographicOperations.FixedTimeEquals(key, keyToCheck);
        
        return verified;
    }
}