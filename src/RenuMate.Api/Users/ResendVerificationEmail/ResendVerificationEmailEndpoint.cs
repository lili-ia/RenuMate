using System.Net.Mime;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.ResendVerificationEmail;

public class ResendVerificationEmailEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/resend-verification", Handle)
        .RequireAuthorization()
        .WithSummary("Resend verification email")
        .WithDescription("Triggers a request to Auth0 to resend the verification email to the current user. " +
                         "Includes a 60-second server-side cooldown.")
        .WithTags("Users")
        .Produces<MessageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<IResult> Handle(
        IUserContext userContext,
        RenuMateDbContext db,
        ILogger<ResendVerificationEmailEndpoint> logger,
        IAuth0Service auth0Service,
        TimeProvider timeProvider,
        IMemoryCache cache,
        CancellationToken cancellationToken = default)
    {
        var auth0Id = userContext.Auth0Id;
        
        var cacheKey = $"cooldown_{auth0Id}";
        
        if (cache.TryGetValue(cacheKey, out _))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Request cooldown active", 
                detail: "Please wait before requesting another link.");
        }

        await auth0Service.ResendVerificationEmail(auth0Id, cancellationToken);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));

        cache.Set(cacheKey, true, cacheOptions);

        return Results.Ok(new MessageResponse("Verification email successfully sent."));
    }
}