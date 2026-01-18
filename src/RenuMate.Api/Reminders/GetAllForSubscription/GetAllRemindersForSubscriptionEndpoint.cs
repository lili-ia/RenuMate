using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.DTOs;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Reminders.GetAllForSubscription;

public abstract class GetAllRemindersForSubscriptionEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/reminders", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Get all reminders for a subscription.")
        .WithDescription("Retrieves all reminder rules associated with a subscription for the authenticated user.")
        .WithTags("Reminders")
        .Produces<List<ReminderDto>>(200, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    
    private static async Task<IResult> Handle(
        [FromQuery] Guid subscriptionId,
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var subscriptionExists = await db.Subscriptions
            .AnyAsync(s => s.Id == subscriptionId && s.UserId == userId, cancellationToken);

        if (!subscriptionExists)
        {
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription exists with the specified ID for this user."
            );
        }

        var reminders = await db.ReminderRules
            .Where(r => r.SubscriptionId == subscriptionId)
            .Select(ReminderMapper.ProjectToDto)
            .ToListAsync(cancellationToken);

        return Results.Ok(reminders);
    }
}