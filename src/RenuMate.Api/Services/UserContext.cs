using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Services;

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