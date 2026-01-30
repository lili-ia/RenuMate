namespace RenuMate.Api.Subscriptions.Create;

public sealed record CreateSubscriptionRequest
(
    string Name,
    string Plan,
    int? CustomPeriodInDays,
    int? TrialPeriodInDays,
    DateOnly StartDate,
    decimal Cost,
    string Currency,
    List<Guid> TagIds,
    string? Note,
    string? CancelLink,
    string? PicLink
);