using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.SetMuteStatus;

public abstract class SetSubscriptionMuteStatusEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPatch("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Set subscription mute status.")
        .WithDescription("Updates the IsMuted flag for a subscription owned by the authenticated user.")
        .WithTags("Subscriptions")
        .Produces(204)  
        .Produces(401)
        .Produces(404) 
        .Produces(500); 
    
     private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromBody] SetSubscriptionMuteStatusRequest request,
        IUserContext userContext,
        RenuMateDbContext db,
        ILogger<SetSubscriptionMuteStatusEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: 401,
                title: "Unauthorized",
                detail: "User is not authenticated."
            );
        }
        
        try
        {
            var rows = await db.Subscriptions
                .Where(s => s.Id == id && s.UserId == userId)
                .ExecuteUpdateAsync(setter =>
                    setter.SetProperty(s => s.IsMuted, request.IsMuted), cancellationToken);

            if (rows == 0)
            {
                return Results.Problem(
                    statusCode: 404,
                    title: "Subscription not found",
                    detail: "No subscription exists with the specified ID for the current user."
                );
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while setting mute status for subscription {SubscriptionId}.", id);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while setting mute status."
            );
        }
    }
}