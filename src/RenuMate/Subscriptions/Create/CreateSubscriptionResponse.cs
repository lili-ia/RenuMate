namespace RenuMate.Subscriptions.Create;

public record CreateSubscriptionResponse(
    Guid Id,
    string Name,
    DateTime RenewalDate,
    string Cost,
    string? Note,
    string? CancelLink,
    string? PicLink
);