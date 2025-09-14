using Microsoft.EntityFrameworkCore;
using RenuMate.EventHandlers;
using RenuMate.Events;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly RenuMateDbContext _db;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IEventHandler _eventHandler;
    
    public SubscriptionService(RenuMateDbContext db, ILogger<SubscriptionService> logger, IEventHandler eventHandler)
    {
        _db = db;
        _logger = logger;
        _eventHandler = eventHandler;
    }

    public async Task ProcessSubscriptionRenewalAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;

        var subscriptions = await _db.Subscriptions
            .Where(s => s.RenewalDate.Date == now)
            .ToListAsync(cancellationToken);

        foreach (var s in subscriptions)
        {
            s.Renew();
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var s in subscriptions)
        {
            foreach (var ev in s.DomainEvents)
            {
                if (ev is SubscriptionRenewedEvent renewed)
                {
                    await _eventHandler.HandleAsync(renewed, cancellationToken);
                }
            }
            s.ClearDomainEvents();
        }

        _logger.LogInformation(
            "Successfully updated {SubscriptionCount} subscriptions renewal date.", 
            subscriptions.Count);
    }
}