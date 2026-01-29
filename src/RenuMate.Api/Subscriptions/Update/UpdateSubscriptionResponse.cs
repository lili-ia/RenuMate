namespace RenuMate.Api.Subscriptions.Update;

public record UpdateSubscriptionResponse(
    Guid Id,
    string Name,
    DateOnly RenewalDate,
    string Cost,
    string? Note,
    string? CancelLink,
    string? PicLink
);