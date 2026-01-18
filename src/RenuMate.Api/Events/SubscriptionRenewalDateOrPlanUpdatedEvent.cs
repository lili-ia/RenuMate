using MediatR;

namespace RenuMate.Api.Events;

public sealed record SubscriptionRenewalDateOrPlanUpdatedEvent(
    Guid SubscriptionId, 
    DateTime RenewalDate) : INotification;