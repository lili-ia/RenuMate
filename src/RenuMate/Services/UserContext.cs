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

            var id = user?.GetUserInfo(config).UserId;

            if (id is null)
            {
                throw new UnauthorizedAccessException("User context is missing or invalid.");
            }

            return id.Value;
        }
    }
}