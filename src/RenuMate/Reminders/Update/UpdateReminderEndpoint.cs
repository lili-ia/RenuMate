using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.Update;

public class UpdateReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app.
        MapPut("api/subscriptions/{subscriptionId:guid}/reminders/{reminderId:guid}", Handle)
        .RequireAuthorization("EmailConfirmed");
    
     private static async Task<IResult> Handle(
        [FromRoute] Guid reminderId,
        [FromRoute] Guid subscriptionId,
        [FromBody] UpdateReminderRequest request,
        [FromServices] RenuMateDbContext db,
        [FromServices] IValidator<UpdateReminderRequest> validator,
        [FromServices] IUserContext userContext,
        [FromServices] ILogger<UpdateReminderEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
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
            return Results.NotFound("Subscription not found.");
        }

        var similarExists = await db.ReminderRules
            .AnyAsync(r => r.SubscriptionId == subscriptionId
                           && r.Id != reminderId
                           && r.DaysBeforeRenewal == request.DaysBeforeRenewal
                           && r.NotifyTimeUtc == request.NotifyTime, cancellationToken);

        if (similarExists)
        {
            return Results.Conflict("Similar reminder rule already exists.");
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
                return Results.NotFound("Reminder not found.");
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
            logger.LogError(
                ex, "Error while creating reminder for subscription {SubscriptionId}.", subscriptionId);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}

public class UpdateReminderResponse
{
    public Guid Id { get; set; }
    
    public Guid SubscriptionId { get; set; }

    public int DaysBeforeRenewal { get; set; }  
    
    public TimeSpan NotifyTime { get; set; } 
    
    public DateTime NextReminder { get; set; } 
}