using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.GetDetailsById;

public class GetSubscriptionDetailsByIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/{id:guid}", Handle);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
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
            .Where(s => s.Id == id && s.UserId == userId)
            .Select(SubscriptionMapper.ProjectToDetailsDto)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Results.NotFound("Subscription not found.");
        }

        return Results.Ok(subscription);
    }
}