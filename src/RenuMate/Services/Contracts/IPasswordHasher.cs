namespace RenuMate.Services.Contracts;

public interface IPasswordHasher
{
    string HashPassword(string raw);

    bool VerifyHashedPassword(string raw, string hashed);
}