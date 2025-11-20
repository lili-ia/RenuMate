using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Delete;

public abstract class DeleteSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Delete a subscription.")
        .WithDescription("Deletes the subscription with the specified ID for the authenticated user.")
        .WithTags("Subscriptions")
        .Produces(204) 
        .Produces(401) 
        .Produces(404)
        .Produces(500);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        IUserContext userContext,
        RenuMateDbContext db,
        ILogger<DeleteSubscriptionEndpoint> logger,
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
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

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
            logger.LogError(ex, "Error while deleting subscription {SubscriptionId}.", id);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while deleting the subscription."
            );
        }
    }
}