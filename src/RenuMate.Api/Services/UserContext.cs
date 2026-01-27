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

            if (id is null || id.Value == Guid.Empty)
            {
                throw new UnauthorizedAccessException("User context is missing or invalid.");
            }
            
            return id.Value;
        }
    }
    
    public string Auth0Id 
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            var auth0Id = user?.GetUserInfo(config).Auth0Id;

            if (auth0Id is null || string.IsNullOrEmpty(auth0Id))
            {
                throw new UnauthorizedAccessException("User context is missing or invalid.");
            }
            
            return auth0Id;
        }
    }
}