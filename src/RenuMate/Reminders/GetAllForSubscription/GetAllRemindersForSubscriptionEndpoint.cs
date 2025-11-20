using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.DTOs;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.GetAllForSubscription;

public abstract class GetAllRemindersForSubscriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/{subscriptionId:guid}/reminders", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Get all reminders for a subscription.")
        .WithDescription("Retrieves all reminder rules associated with a subscription for the authenticated user.")
        .WithTags("Reminders")
        .Produces<List<ReminderDto>>(200, "application/json")
        .Produces(401)
        .Produces(403)
        .Produces(404);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid subscriptionId,
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
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

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == subscriptionId)
            .Select(s => new { s.Id, s.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription exists with the specified ID."
            );
        }

        if (subscription.UserId != userId)
        {
            return Results.Problem(
                statusCode: 403,
                title: "Forbidden",
                detail: "You do not have access to this subscription."
            );
        }
        
        var reminders = await db.ReminderRules
            .Where(r => r.SubscriptionId == subscriptionId)
            .Select(ReminderMapper.ProjectToDto)
            .ToListAsync(cancellationToken);

        return Results.Ok(reminders);
    }
}