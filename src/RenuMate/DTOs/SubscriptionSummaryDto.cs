namespace RenuMate.DTOs;

public sealed record SubscriptionSummaryDto(
    decimal TotalCost,
    decimal ProjectedCost,
    int ActiveSubscriptionsCount,
    int ActiveRemindersCount
);