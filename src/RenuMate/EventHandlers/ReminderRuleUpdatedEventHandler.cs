using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Events;
using RenuMate.Persistence;

namespace RenuMate.EventHandlers;

public class ReminderRuleUpdatedEventHandler(RenuMateDbContext db) : INotificationHandler<ReminderRuleUpdatedEvent>
{
    public async Task Handle(ReminderRuleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var occurrences = await db.ReminderOccurrences
            .Include(o => o.ReminderRule)
            .ThenInclude(r => r.Subscription)
            .Where(o => o.ReminderRule.Id == notification.ReminderRuleId && !o.IsSent)
            .ToListAsync(cancellationToken);
        
        foreach (var o in occurrences)
        {
            o.ScheduledAt = o.ReminderRule.Subscription.RenewalDate
                .AddDays(-notification.DaysBeforeRenewal)
                .Add(notification.NotifyTime);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}