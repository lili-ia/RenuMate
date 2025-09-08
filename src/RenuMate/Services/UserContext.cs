using System.Security.Claims;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user?.FindFirst("nameidentifier")?.Value;
            
            return Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
        }
    }
}