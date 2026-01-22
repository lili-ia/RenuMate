using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Reminders.Create;

public abstract class CreateReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/reminders", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .AddEndpointFilter<InvalidateSummaryCacheEndpointFilter>()
        .WithSummary("Creates a new reminder for a subscription.")
        .WithDescription("Adds a reminder rule for a given subscription. A maximum of three reminder rules can be created per subscription.")
        .WithTags("Reminders")
        .Produces<CreateReminderResponse>(200, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);
    
    private static async Task<IResult> Handle(
        [FromBody] CreateReminderRequest request,
        RenuMateDbContext db,
        IValidator<CreateReminderRequest> validator,
        IUserContext userContext,
        ILogger<CreateReminderEndpoint> logger,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var subscription = await db.Subscriptions
            .Where(s => s.Id == request.SubscriptionId && s.UserId == userId)
            .Include(s => s.Reminders)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription found with the specified ID for the current user."
            );
        }
        
        try
        {
            var notifyTime = request.GetUtcNotifyTime();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            subscription.AddReminderRule(notifyTime, request.DaysBeforeRenewal, now);
            await db.SaveChangesAsync(cancellationToken);

            var newRule = subscription.Reminders.Last();

            return Results.Ok(new CreateReminderResponse
            (
                newRule.Id,
                newRule.SubscriptionId,
                newRule.DaysBeforeRenewal,
                newRule.NotifyTimeUtc
            ));
        }
        catch (TimeZoneNotFoundException)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid timezone",
                detail: $"The timezone '{request.Timezone}' is not valid."
            );
        }
    }
}