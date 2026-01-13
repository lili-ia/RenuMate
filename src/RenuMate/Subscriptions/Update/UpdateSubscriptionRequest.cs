namespace RenuMate.Subscriptions.Update;

public sealed record UpdateSubscriptionRequest(
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