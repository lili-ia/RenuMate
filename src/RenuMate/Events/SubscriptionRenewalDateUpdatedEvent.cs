using MediatR;

namespace RenuMate.Events;

public sealed record SubscriptionRenewalDateUpdatedEvent(
    Guid SubscriptionId, 
    DateTime RenewalDate) : INotification;