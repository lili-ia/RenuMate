using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.GetInfo;

public abstract class GetUserInfoEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/users/me", Handle)
        .RequireAuthorization()
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
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var cacheKey = $"info_user_{userId}";

        if (cache.TryGetValue(cacheKey, out UserInfoResponse? info))
        {
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
            return Results.Problem(
                statusCode: 404,
                title: "User not found",
                detail: "The authenticated user could not be found in the database."
            );
        }

        cache.Set(cacheKey, info, TimeSpan.FromHours(24));
        
        return Results.Ok(info);
    }
}
