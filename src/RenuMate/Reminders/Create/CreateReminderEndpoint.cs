using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.Create;

public class CreateReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/subscriptions/{subscriptionId:guid}/reminders", Handle)
        .RequireAuthorization("EmailConfirmed");
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid subscriptionId,
        [FromBody] CreateReminderRequest request,
        [FromServices] RenuMateDbContext db,
        [FromServices] IValidator<CreateReminderRequest> validator,
        [FromServices] IUserContext userContext,
        [FromServices] ILogger<CreateReminderEndpoint> logger,
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
                           && r.DaysBeforeRenewal == request.DaysBeforeRenewal
                           && r.NotifyTime == request.NotifyTime, cancellationToken);

        if (similarExists)
        {
            return Results.Conflict("Similar reminder rule already exists.");
        }

        var remindersCount = await db.ReminderRules
            .Where(r => r.SubscriptionId == subscriptionId)
            .CountAsync(cancellationToken);

        if (remindersCount == 3)
        {
            return Results.Conflict("You can not create more than 3 reminders for each subscription.");
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
        
        var reminderRule = new ReminderRule
        {
            SubscriptionId = subscriptionId,
            DaysBeforeRenewal = request.DaysBeforeRenewal,
            NotifyTime = request.NotifyTime
        };

        var nextReminderOccurrence = new ReminderOccurrence
        {
            ReminderRuleId = reminderRule.Id,
            ScheduledAt = nextReminder,
            IsSent = false
        };
        
        try
        {
            await db.ReminderRules.AddAsync(reminderRule, cancellationToken);
            await db.ReminderOccurrences.AddAsync(nextReminderOccurrence, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            
            return Results.Ok(new CreateReminderResponse
            {
                Id = reminderRule.Id,
                SubscriptionId = reminderRule.SubscriptionId,
                DaysBeforeRenewal = reminderRule.DaysBeforeRenewal,
                NotifyTime = reminderRule.NotifyTime,
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

public class CreateReminderResponse
{
    public Guid Id { get; set; }
    
    public Guid SubscriptionId { get; set; }

    public int DaysBeforeRenewal { get; set; }  
    
    public TimeSpan NotifyTime { get; set; } 
    
    public DateTime NextReminder { get; set; } 
}