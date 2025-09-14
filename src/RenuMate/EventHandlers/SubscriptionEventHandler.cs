using Microsoft.EntityFrameworkCore;
using RenuMate.Events;
using RenuMate.Persistence;

namespace RenuMate.EventHandlers;

public class SubscriptionEventHandler(RenuMateDbContext db)
{
    public async Task HandleAsync(SubscriptionRenewedEvent @event, CancellationToken cancellationToken = default)
    {
        var subscription = @event.Subscription;

        var reminders = await db.Reminders
            .Where(r => r.SubscriptionId == subscription.Id)
            .ToListAsync(cancellationToken);

        foreach (var r in reminders)
        {
            r.NextReminder = subscription.RenewalDate
                .AddDays(-r.DaysBeforeRenewal)
                .Add(r.NotifyTime);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}