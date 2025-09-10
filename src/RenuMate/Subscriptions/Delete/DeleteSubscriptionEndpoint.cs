using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Delete;

public class DeleteSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/subscriptions/{id:guid}", Handle);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromServices] IUserContext userContext,
        [FromServices] RenuMateDbContext db,
        [FromServices] ILogger<DeleteSubscriptionEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        try
        {
            var rows = await db.Subscriptions
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows == 0)
            {
                return Results.NotFound("Subscription not found.");
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deleting subscription {SubscriptionId}.", id);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}