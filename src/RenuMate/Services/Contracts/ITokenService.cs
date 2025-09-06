using System.Security.Claims;

namespace RenuMate.Services.Contracts;

public interface ITokenService
{
    string CreateToken(string userId, string email, string purpose, DateTime expiresAt);

    ClaimsPrincipal? ValidateToken(string token, string expectedPurpose);
}