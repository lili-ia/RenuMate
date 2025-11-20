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

public abstract class CreateReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/subscriptions/{subscriptionId:guid}/reminders", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Creates a new reminder for a subscription.")
        .WithDescription("Adds a reminder rule for a given subscription. A maximum of three reminder rules can be created per subscription.")
        .WithTags("Reminders")
        .Produces<CreateReminderResponse>(200, "application/json")
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(409)
        .Produces(500);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid subscriptionId,
        [FromBody] CreateReminderRequest request,
        RenuMateDbContext db,
        IValidator<CreateReminderRequest> validator,
        IUserContext userContext,
        ILogger<CreateReminderEndpoint> logger,
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
                detail: "No subscription found with the specified ID for the current user."
            );
        }
        
        TimeZoneInfo userTz;
        try
        {
            userTz = TimeZoneInfo.FindSystemTimeZoneById(request.Timezone);
        }
        catch (Exception)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid timezone",
                detail: $"The timezone '{request.Timezone}' is not valid."
            );
        }
        
        var today = DateTime.Today;

        var localDateTime = new DateTime(
            today.Year, today.Month, today.Day,
            request.NotifyTime.Hours,
            request.NotifyTime.Minutes,
            request.NotifyTime.Seconds,
            DateTimeKind.Unspecified);

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, userTz);
        var utcTime = utcDateTime.TimeOfDay;
        
        var similarExists = await db.ReminderRules
            .AnyAsync(r => r.SubscriptionId == subscriptionId
                           && r.DaysBeforeRenewal == request.DaysBeforeRenewal
                           && r.NotifyTimeUtc == utcTime, cancellationToken);

        if (similarExists)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Reminder already exists",
                detail: "A reminder with the same time and days-before-renewal already exists for this subscription."
            );
        }

        var remindersCount = await db.ReminderRules
            .Where(r => r.SubscriptionId == subscriptionId)
            .CountAsync(cancellationToken);

        if (remindersCount >= 3)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Maximum reminders reached",
                detail: "You cannot create more than 3 reminders for a single subscription."
            );
        }

        var nextReminder = subscription.RenewalDate
            .AddDays(-request.DaysBeforeRenewal)
            .Add(utcTime);
            
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
            NotifyTimeUtc = utcTime
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
                NotifyTime = reminderRule.NotifyTimeUtc,
                NextReminder = nextReminder
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating reminder for subscription {SubscriptionId}.", subscriptionId);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while creating the reminder."
            );
        }
    }
}