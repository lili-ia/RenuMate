using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Events;
using RenuMate.Persistence;

namespace RenuMate.EventHandlers;

public class SubscriptionRenewalDateUpdatedEventHandler(RenuMateDbContext db) : INotificationHandler<SubscriptionRenewalDateUpdatedEvent>
{
    public async Task Handle(SubscriptionRenewalDateUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var occurrences = await db.ReminderOccurrences
            .Include(r => r.ReminderRule)
            .Where(o => o.ReminderRule.SubscriptionId == notification.SubscriptionId && !o.IsSent)
            .ToListAsync(cancellationToken);

        foreach (var r in occurrences)
        {
            r.ScheduledAt = notification.RenewalDate
                .AddDays(-r.ReminderRule.DaysBeforeRenewal)
                .Add(r.ReminderRule.NotifyTime);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}