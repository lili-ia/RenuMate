using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        .Produces<UserInfoResponse>(200, "application/json")
        .Produces(401)
        .Produces(404);

    private static async Task<IResult> Handle(
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var info = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserInfoResponse
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                MemberSince = u.CreatedAt,
                SubscriptionCount = u.Subscriptions.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null)
        {
            return Results.NotFound(new { Error = "User not found."});
        }
        
        return Results.Ok(info);
    }
}
