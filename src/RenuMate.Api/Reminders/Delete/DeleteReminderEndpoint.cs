using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Reminders.Delete;

public abstract class DeleteReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/reminders/{id:guid}", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .AddEndpointFilter<InvalidateSummaryCacheEndpointFilter>()
        .WithSummary("Deletes a reminder.")
        .WithDescription("Deletes a specific reminder rule for a given subscription belonging to the authenticated user.")
        .WithTags("Reminders")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        RenuMateDbContext db,
        IUserContext userContext,
        ILogger<DeleteReminderEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var rule = await db.ReminderRules
            .Include(r => r.ReminderOccurrences)
            .Where(r => r.Id == id && r.Subscription.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (rule is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Reminder not found",
                detail: "No reminder exists with the specified ID for this subscription.");
        }
        
        db.ReminderRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
    
}