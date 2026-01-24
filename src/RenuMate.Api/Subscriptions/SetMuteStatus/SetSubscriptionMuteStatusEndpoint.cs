using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.SetMuteStatus;

public abstract class SetSubscriptionMuteStatusEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPatch("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .AddEndpointFilter<InvalidateSummaryCacheEndpointFilter>()
        .WithSummary("Set subscription mute status.")
        .WithDescription("Updates the IsMuted flag for a subscription owned by the authenticated user.")
        .WithTags("Subscriptions")
        .Produces(StatusCodes.Status204NoContent)  
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound) 
        .Produces(StatusCodes.Status500InternalServerError); 
    
     private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromBody] SetSubscriptionMuteStatusRequest request,
        IUserContext userContext,
        RenuMateDbContext db,
        ILogger<SetSubscriptionMuteStatusEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userContext.UserId, cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription exists with the specified ID for the current user."
            );
        }

        subscription.SetMuteStatus(request.IsMuted);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}