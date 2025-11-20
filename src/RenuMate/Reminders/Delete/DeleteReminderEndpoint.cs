using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.Delete;

public abstract class DeleteReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/subscriptions/{subscriptionId:guid}/reminders/{reminderId:guid}", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Deletes a reminder.")
        .WithDescription("Deletes a specific reminder rule for a given subscription belonging to the authenticated user.")
        .WithTags("Reminders")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid reminderId,
        RenuMateDbContext db,
        IUserContext userContext,
        ILogger<DeleteReminderEndpoint> logger,
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
            var rows = await db.ReminderRules
                .Where(r => r.Id == reminderId 
                            && r.SubscriptionId == subscriptionId
                            && r.Subscription.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows == 0)
            {
                return Results.Problem(
                    statusCode: 404,
                    title: "Reminder not found",
                    detail: "No reminder exists with the specified ID for this subscription."
                );
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deleting reminder {ReminderId}.", reminderId);

            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while deleting the reminder."
            );
        }
    }
}