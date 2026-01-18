namespace RenuMate.Api.Subscriptions.Update;

public record UpdateSubscriptionResponse(
    Guid Id,
    string Name,
    DateTime RenewalDate,
    string Cost,
    string? Note,
    string? CancelLink,
    string? PicLink
);