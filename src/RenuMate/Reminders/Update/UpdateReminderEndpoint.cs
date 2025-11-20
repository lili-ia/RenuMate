using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.Update;

public abstract class UpdateReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPut("api/subscriptions/{subscriptionId:guid}/reminders/{reminderId:guid}", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Updates a reminder.")
        .WithDescription("Updates the notify time or days-before-renewal for a specific reminder of a subscription.")
        .WithTags("Reminders")
        .Produces<UpdateReminderResponse>(200, "application/json")
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(409)
        .Produces(500);
    
     private static async Task<IResult> Handle(
        [FromRoute] Guid reminderId,
        [FromRoute] Guid subscriptionId,
        [FromBody] UpdateReminderRequest request,
        RenuMateDbContext db,
        IValidator<UpdateReminderRequest> validator,
        IUserContext userContext,
        ILogger<UpdateReminderEndpoint> logger,
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
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == subscriptionId && s.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription exists with the specified ID for the current user."
            );
        }

        var similarExists = await db.ReminderRules
            .AnyAsync(r => r.SubscriptionId == subscriptionId
                           && r.Id != reminderId
                           && r.DaysBeforeRenewal == request.DaysBeforeRenewal
                           && r.NotifyTimeUtc == request.NotifyTime, cancellationToken);

        if (similarExists)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Reminder conflict",
                detail: "A reminder with the same time and days-before-renewal already exists for this subscription."
            );
        }

        var nextReminder = subscription.RenewalDate
            .AddDays(-request.DaysBeforeRenewal)
            .Add(request.NotifyTime);
            
        while (nextReminder <= DateTime.UtcNow)
        {
            nextReminder = subscription.Plan switch
            {
                SubscriptionPlan.Monthly => nextReminder.AddMonths(1),
                SubscriptionPlan.Quarterly => nextReminder.AddMonths(3),
                SubscriptionPlan.Annual => nextReminder.AddYears(1),
                SubscriptionPlan.Custom when subscription.CustomPeriodInDays.HasValue 
                    => nextReminder.AddDays(subscription.CustomPeriodInDays.Value),
                _ => throw new InvalidOperationException("Unknown subscription plan")
            };
        }
        
        try
        {
            var rows = await db.ReminderRules
                .Where(r => r.Id == reminderId)
                .ExecuteUpdateAsync(
                    setter => setter
                        .SetProperty(r => r.NotifyTimeUtc, request.NotifyTime)
                        .SetProperty(r => r.DaysBeforeRenewal, request.DaysBeforeRenewal),
                    cancellationToken);

            if (rows == 0)
            {
                return Results.Problem(
                    statusCode: 404,
                    title: "Reminder not found",
                    detail: "No reminder exists with the specified ID for this subscription."
                );
            }
            
            return Results.Ok(new UpdateReminderResponse
            {
                Id = reminderId,
                SubscriptionId = subscriptionId,
                DaysBeforeRenewal = request.DaysBeforeRenewal,
                NotifyTime = request.NotifyTime,
                NextReminder = nextReminder
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating reminder for subscription {SubscriptionId}.", subscriptionId);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while updating the reminder."
            );
        }
    }
}