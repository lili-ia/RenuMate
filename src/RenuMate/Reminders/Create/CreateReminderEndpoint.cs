using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.Create;

public abstract class CreateReminderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/reminders", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
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
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == request.SubscriptionId && s.UserId == userId)
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
            .AnyAsync(r => r.SubscriptionId == request.SubscriptionId
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
            .Where(r => r.SubscriptionId == request.SubscriptionId)
            .CountAsync(cancellationToken);

        if (remindersCount >= 3)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Maximum reminders reached",
                detail: "You cannot create more than 3 reminders for a single subscription."
            );
        }
        
        var reminderRule = new ReminderRule
        {
            SubscriptionId = request.SubscriptionId,
            DaysBeforeRenewal = request.DaysBeforeRenewal,
            NotifyTimeUtc = utcTime
        };
        
        var scheduledTime = reminderRule.CalculateNextOccurrence(
            subscription.RenewalDate, 
            subscription.Plan, 
            subscription.CustomPeriodInDays);

        var nextReminderOccurrence = new ReminderOccurrence
        {
            ReminderRuleId = reminderRule.Id,
            ScheduledAt = scheduledTime,
            IsSent = false
        };
        
        try
        {
            await db.ReminderRules.AddAsync(reminderRule, cancellationToken);
            await db.ReminderOccurrences.AddAsync(nextReminderOccurrence, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            
            return Results.Ok(new CreateReminderResponse
            (
                reminderRule.Id,
                reminderRule.SubscriptionId,
                reminderRule.DaysBeforeRenewal,
                reminderRule.NotifyTimeUtc,
                scheduledTime
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating reminder for subscription {SubscriptionId}.", request.SubscriptionId);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while creating the reminder."
            );
        }
    }
}