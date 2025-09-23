using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.SetMuteStatus;

public class SetSubscriptionMuteStatusEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPatch("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("EmailConfirmed");
    
     private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromBody] SetSubscriptionMuteStatusRequest request,
        [FromServices] IUserContext userContext,
        [FromServices] RenuMateDbContext db,
        [FromServices] ILogger<SetSubscriptionMuteStatusEndpoint> logger,
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
                .Where(s => s.Id == id && s.UserId == userId)
                .ExecuteUpdateAsync(setter =>
                    setter.SetProperty(s => s.IsMuted, request.IsMuted), cancellationToken);

            if (rows == 0)
            {
                return Results.NotFound("Subscription not found.");
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while setting mute status for subscription {SubscriptionId}.", id);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}