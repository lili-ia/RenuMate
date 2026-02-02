using RenuMate.Api.Enums;

namespace RenuMate.Api.Subscriptions.Create;

public record CreateSubscriptionResponse(
    Guid Id,
    string Name,
    DateOnly RenewalDate,
    SubscriptionPlan Plan,
    string Cost,
    List<Guid> TagIds,
    string? Note,
    string? CancelLink,
    string? PicLink
);