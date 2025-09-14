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
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("api/subscriptions/{subscriptionId:guid}/reminders", Handle);
    
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

        var nextReminder = subscription.RenewalDate
            .AddDays(-request.DaysBeforeRenewal)
            .Add(request.NotifyTime);
            
        while (nextReminder <= DateTime.UtcNow)
        {
            nextReminder = subscription.Type switch
            {
                SubscriptionType.Monthly => nextReminder.AddMonths(1),
                SubscriptionType.Quarterly => nextReminder.AddMonths(3),
                SubscriptionType.Annual => nextReminder.AddYears(1),
                SubscriptionType.Custom when subscription.CustomPeriodInDays.HasValue 
                    => nextReminder.AddDays(subscription.CustomPeriodInDays.Value),
                _ => throw new InvalidOperationException("Unknown subscription type")
            };
        }
        
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            SubscriptionId = subscriptionId,
            DaysBeforeRenewal = request.DaysBeforeRenewal,
            NotifyTime = request.NotifyTime,
            IsMuted = false,
            NextReminder = nextReminder
        };
        
        try
        {
            await db.Reminders.AddAsync(reminder, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            
            return Results.Ok(new CreateReminderResponse
            {
                Id = reminder.Id,
                SubscriptionId = reminder.SubscriptionId,
                DaysBeforeRenewal = reminder.DaysBeforeRenewal,
                NotifyTime = reminder.NotifyTime,
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