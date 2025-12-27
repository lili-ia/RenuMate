using System.Security.Claims;

namespace RenuMate.Extensions;

public static class ClaimsExtensions
{
    public static (string Auth0Id, Guid UserId, string Email, string Name, bool IsVerified) 
        GetUserInfo(this ClaimsPrincipal user, IConfiguration config)
    {
        return (
            Auth0Id: user.FindFirstValue(ClaimTypes.NameIdentifier)?? string.Empty,
            UserId: Guid.TryParse(user.FindFirstValue($"{config["Auth0:Namespace"]}/user_id"), out var id) ? id : Guid.Empty,
            Email: user.FindFirstValue($"{config["Auth0:Namespace"]}/email") ?? string.Empty,
            Name: user.FindFirstValue($"{config["Auth0:Namespace"]}/name") ?? user.FindFirstValue("name") ?? string.Empty,
            IsVerified: bool.TryParse(user.FindFirstValue($"{config["Auth0:Namespace"]}/email_verified"), out var v) && v
        );
    }
}