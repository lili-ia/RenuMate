using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Services;

public class SubscriptionService(RenuMateDbContext db, ILogger<SubscriptionService> logger, TimeProvider timeProvider) 
    : ISubscriptionService
{
    public async Task ProcessSubscriptionRenewalAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var subscriptions = await db.Subscriptions
            .Where(s => s.RenewalDate.Date <= now)
            .ToListAsync(cancellationToken);

        foreach (var s in subscriptions)
        {
            s.UpdateNextRenewalDate(now);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully updated {SubscriptionCount} subscriptions renewal date.", 
            subscriptions.Count);
    }
}