namespace RenuMate.Reminders.Create;

public sealed record CreateReminderResponse
(
    Guid Id,
    Guid SubscriptionId,
    int DaysBeforeRenewal,
    TimeSpan NotifyTime
);