using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.GetAllForSubscription;

public class GetAllRemindersForSubscriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("api/subscriptions/{subscriptionId:guid}/reminders", Handle);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid subscriptionId,
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == subscriptionId)
            .Select(s => new { s.Id, s.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Results.NotFound("Subscription not found.");
        }

        if (subscription.UserId != userId)
        {
            return Results.Forbid();
        }
        
        var reminders = await db.Reminders
            .Where(r => r.SubscriptionId == subscriptionId)
            .Select(ReminderMapper.ProjectToDto)
            .ToListAsync(cancellationToken);

        return Results.Ok(reminders);
    }
}