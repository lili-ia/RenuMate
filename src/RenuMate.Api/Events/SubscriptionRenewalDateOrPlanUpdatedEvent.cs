using MediatR;

namespace RenuMate.Api.Events;

public sealed record SubscriptionRenewalDateOrPlanUpdatedEvent(
    Guid SubscriptionId, 
    DateOnly RenewalDate) : INotification;