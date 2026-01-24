using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace RenuMate.Api.Extensions;

public class CustomAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next, 
        HttpContext context,
        AuthorizationPolicy policy, 
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var reason = context.Items["ForbiddenReason"] as string;

            var problem = reason switch
            {
                "UserNotActive" => new ProblemDetails
                {
                    Title = "Account Deactivated",
                    Status = 403,
                    Detail = "Your account is currently inactive.",
                    Instance = context.Request.Path
                },
                "EmailNotVerified" => new ProblemDetails
                {
                    Title = "Email Verification Required",
                    Status = 403,
                    Detail = "Please verify your email.",
                    Instance = context.Request.Path
                },
                _ => new ProblemDetails
                {
                    Title = "Access Denied",
                    Status = 403,
                    Detail = "You do not have permission.",
                    Instance = context.Request.Path
                }
            };

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}