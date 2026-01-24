using Microsoft.AspNetCore.Authorization;

namespace RenuMate.Api.Middleware;

public class VerifiedEmailOnlyAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<EmailVerifiedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        EmailVerifiedRequirement requirement)
    {
        var emailVerifiedClaim = context.User
            .FindFirst(c => c.Type == "http://renumate.online/email_verified")?.Value;

        if (emailVerifiedClaim != null && emailVerifiedClaim.ToLower() == "true")
        {
            context.Succeed(requirement);
        }
        else
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Items["ForbiddenReason"] = "EmailNotVerified";
            }
        }

        return Task.CompletedTask;
    }
}

public class EmailVerifiedRequirement : IAuthorizationRequirement
{
}