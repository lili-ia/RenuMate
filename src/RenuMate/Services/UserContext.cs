using RenuMate.Extensions;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class UserContext(IHttpContextAccessor httpContextAccessor, IConfiguration config) : IUserContext
{
    public Guid UserId 
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            
            return user?.GetUserInfo(config).UserId ?? Guid.Empty;
        }
    }
}