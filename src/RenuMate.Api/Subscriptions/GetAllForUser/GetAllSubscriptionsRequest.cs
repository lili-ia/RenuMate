namespace RenuMate.Api.Subscriptions.GetAllForUser;

public sealed record GetAllSubscriptionsRequest(
    string? SortOrder,
    string? SortBy,
    int Page = 1,
    int PageSize = 10);