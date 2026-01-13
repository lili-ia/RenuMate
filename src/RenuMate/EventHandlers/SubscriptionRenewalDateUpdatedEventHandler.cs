using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Events;
using RenuMate.Persistence;

namespace RenuMate.EventHandlers;

public class SubscriptionRenewalDateUpdatedEventHandler(RenuMateDbContext db) : INotificationHandler<SubscriptionRenewalDateUpdatedEvent>
{
    public async Task Handle(SubscriptionRenewalDateUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var pendingOccurrences = await db.ReminderOccurrences
            .Include(r => r.ReminderRule)
            .Where(o => o.ReminderRule.SubscriptionId == notification.SubscriptionId && !o.IsSent)
            .ToListAsync(cancellationToken);

        foreach (var occurrence in pendingOccurrences)
        {
            var newScheduledDate = notification.RenewalDate
                .Date
                .AddDays(-occurrence.ReminderRule.DaysBeforeRenewal)
                .Add(occurrence.ReminderRule.NotifyTimeUtc);

            if (occurrence.ScheduledAt != newScheduledDate)
            {
                occurrence.ScheduledAt = newScheduledDate;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}