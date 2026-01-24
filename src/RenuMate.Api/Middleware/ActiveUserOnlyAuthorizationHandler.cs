using Microsoft.AspNetCore.Authorization;

namespace RenuMate.Api.Middleware;

public class ActiveUserOnlyAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<ActiveUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ActiveUserRequirement requirement)
    {
        var isActiveClaim = context.User
            .FindFirst(c => c.Type == "http://renumate.online/is_active")?.Value;

        if (isActiveClaim != null && isActiveClaim.ToLower() == "true")
        {
            context.Succeed(requirement);
        }
        else
        {
            var httpContext = httpContextAccessor.HttpContext;
            
            if (httpContext != null)
            {
                httpContext.Items["ForbiddenReason"] = "UserNotActive";
            }
        }

        return Task.CompletedTask;
    }
}

public class ActiveUserRequirement : IAuthorizationRequirement
{
}