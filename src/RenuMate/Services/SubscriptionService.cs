using Microsoft.EntityFrameworkCore;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class SubscriptionService(RenuMateDbContext db, ILogger<SubscriptionService> logger) : ISubscriptionService
{
    public async Task ProcessSubscriptionRenewalAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;

        var subscriptions = await db.Subscriptions
            .Where(s => s.RenewalDate.Date <= now)
            .ToListAsync(cancellationToken);

        foreach (var s in subscriptions)
        {
            s.UpdateNextRenewalDate();
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully updated {SubscriptionCount} subscriptions renewal date.", 
            subscriptions.Count);
    }
}