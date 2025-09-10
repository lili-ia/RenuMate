using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.GetAllForUser;

public class GetAllSubscriptionsForUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("api/subscriptions", Handle);
    
    private static async Task<IResult> Handle(
        [FromServices] IUserContext userContext,
        [FromServices] RenuMateDbContext db,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(SubscriptionMapper.ProjectToDto)
            .ToListAsync(cancellationToken);

        return Results.Ok(subscription);
    }
}