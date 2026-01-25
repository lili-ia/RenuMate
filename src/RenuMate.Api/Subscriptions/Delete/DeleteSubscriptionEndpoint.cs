using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.Delete;

public abstract class DeleteSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .AddEndpointFilter<InvalidateSummaryCacheEndpointFilter>()
        .WithSummary("Delete a subscription.")
        .WithDescription("Deletes the subscription with the specified ID for the authenticated user.")
        .WithTags("Subscriptions")
        .Produces(StatusCodes.Status204NoContent) 
        .Produces(StatusCodes.Status401Unauthorized) 
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        IUserContext userContext,
        RenuMateDbContext db,
        ILogger<DeleteSubscriptionEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var rows = await db.Subscriptions
            .Where(s => s.Id == id && s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (rows == 0)
        {
            logger.LogInformation("Subscription {SubId} not found by user {UserId}.", id, userId);
            
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription exists with the specified ID for the current user."
            );
        }
        
        logger.LogInformation("User {UserId} successfully deleted subscription {SubId}", userId, id);

        return Results.NoContent();
    }
}