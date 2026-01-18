using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Entities;
using RenuMate.Api.Events;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.EventHandlers;

public class SubscriptionRenewalDateUpdatedEventHandler(RenuMateDbContext db, ILogger<SubscriptionRenewalDateUpdatedEventHandler> logger) 
    : INotificationHandler<SubscriptionRenewalDateOrPlanUpdatedEvent>
{
    public async Task Handle(SubscriptionRenewalDateOrPlanUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Cleaning up old reminders for subscription {SubId}...", notification.SubscriptionId);
    
        var rowsAffected = await db.ReminderOccurrences
            .Where(o => o.ReminderRule.SubscriptionId == notification.SubscriptionId && !o.IsSent)
            .ExecuteDeleteAsync(cancellationToken);
    
        logger.LogInformation("Deleted {Count} obsolete reminders.", rowsAffected);
    
        var reminderRules = await db.ReminderRules
            .AsNoTracking()
            .Where(rr => rr.SubscriptionId == notification.SubscriptionId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var rule in reminderRules)
        {
            var scheduledAt = notification.RenewalDate
                .Date
                .AddDays(-rule.DaysBeforeRenewal)
                .Add(rule.NotifyTimeUtc);
        
            if (scheduledAt > now)
            {
                var occurrence = ReminderOccurrence.Create(rule.Id, scheduledAt);

                await db.ReminderOccurrences.AddAsync(occurrence, cancellationToken);
            }
            else 
            {
                logger.LogDebug("Skipping reminder for rule {RuleId} because its date {Date} is in the past.", 
                    rule.Id, scheduledAt);
            }
        }
  
        await db.SaveChangesAsync(cancellationToken);
    }
}