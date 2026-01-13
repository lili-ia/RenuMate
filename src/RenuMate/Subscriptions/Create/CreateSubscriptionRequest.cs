namespace RenuMate.Subscriptions.Create;

public sealed record CreateSubscriptionRequest
(
    string Name,
    string Plan,
    int? CustomPeriodInDays,
    DateTime StartDate,
    decimal Cost,
    string Currency,
    string? Note,
    string? CancelLink,
    string? PicLink
);