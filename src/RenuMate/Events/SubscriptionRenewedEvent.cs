using RenuMate.Entities;

namespace RenuMate.Events;

public class SubscriptionRenewedEvent(Subscription subscription) : IEvent
{
    public Subscription Subscription { get; } = subscription;
}