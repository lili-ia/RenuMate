using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.Delete;

public class DeleteReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/subscriptions/{subscriptionId:guid}/reminders/{reminderId:guid}", Handle);

    private static async Task<IResult> Handle(
        [FromRoute] Guid subscriptionId,
        [FromRoute] Guid reminderId,
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        [FromServices] ILogger<DeleteReminderEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        try
        {
            var rows = await db.Reminders
                .Where(r => r.Id == reminderId 
                            && r.SubscriptionId == subscriptionId
                            && r.Subscription.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows == 0)
            {
                return Results.NotFound("Reminder not found.");
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deleting reminder {ReminderId}.", reminderId);

            return Results.InternalServerError("An internal error occurred.");
        }
    }
}