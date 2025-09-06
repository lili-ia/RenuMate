namespace RenuMate.Services.Contracts;

public interface IPasswordHasher
{
    public string HashPassword(string raw);

    public bool VerifyHashedPassword(string raw, string hashed);
}