namespace RenuMate.Api.Subscriptions.Update;

public sealed record UpdateSubscriptionRequest(
    string Name,
    string Plan,
    int? CustomPeriodInDays,
    int? TrialPeriodInDays,
    DateTime StartDate,
    decimal Cost,
    string Currency,
    string? Note,
    string? CancelLink,
    string? PicLink
);