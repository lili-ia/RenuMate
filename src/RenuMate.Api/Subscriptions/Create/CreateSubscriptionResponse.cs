namespace RenuMate.Api.Subscriptions.Create;

public record CreateSubscriptionResponse(
    Guid Id,
    string Name,
    DateOnly RenewalDate,
    string Cost,
    string? Note,
    string? CancelLink,
    string? PicLink
);