using Microsoft.EntityFrameworkCore;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly RenuMateDbContext _db;
    private readonly ILogger<SubscriptionService> _logger;
    
    public SubscriptionService(RenuMateDbContext db, ILogger<SubscriptionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessSubscriptionRenewalAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;

        var subscriptions = await _db.Subscriptions
            .Where(s => s.RenewalDate.Date == now)
            .ToListAsync(cancellationToken);

        foreach (var s in subscriptions)
        {
            s.RenewalDate = s.RenewalDate.AddPeriod(s.Plan, s.CustomPeriodInDays);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully updated {SubscriptionCount} subscriptions renewal date.", 
            subscriptions.Count);
    }
}