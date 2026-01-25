using System.ComponentModel.Design.Serialization;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.GetInfo;

public abstract class GetUserInfoEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/users/me", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .WithSummary("Get current user info.")
        .WithDescription("Retrieves details about the authenticated user including email, name, member since, and subscription count.")
        .WithTags("Users")
        .Produces<UserInfoResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(
        RenuMateDbContext db,
        IUserContext userContext,
        IMemoryCache cache,
        ILogger<GetUserInfoEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var cacheKey = $"info_user_{userId}";

        if (cache.TryGetValue(cacheKey, out UserInfoResponse? info))
        {
            logger.LogInformation("User {UserId} info was successfully retrieved from cache.", userId);
            
            return Results.Ok(info);
        }

        info = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserInfoResponse
            (
                u.Id,
                u.Email,
                u.Name,
                u.CreatedAt,
                u.Subscriptions.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null)
        {
            logger.LogWarning("User {UserId} was authorized but not found in database.", userId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: "The authenticated user could not be found in the database."
            );
        }

        cache.Set(cacheKey, info, TimeSpan.FromHours(24));
        
        logger.LogInformation("User {UserId} info was successfully retrieved. Result successfully cached for next 24 hrs.", userId);
        
        return Results.Ok(info);
    }
}
