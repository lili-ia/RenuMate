using MediatR;

namespace RenuMate.Events;

public sealed record SubscriptionRenewalDateOrPlanUpdatedEvent(
    Guid SubscriptionId, 
    DateTime RenewalDate) : INotification;