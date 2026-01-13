namespace RenuMate.DTOs;

public sealed record SubscriptionSummaryDto(
    decimal TotalCost,
    int ActiveSubscriptionsCount,
    int TotalRemindersCount
);