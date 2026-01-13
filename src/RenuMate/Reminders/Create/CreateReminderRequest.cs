namespace RenuMate.Reminders.Create;

public sealed record CreateReminderRequest
(
    Guid SubscriptionId,
    int DaysBeforeRenewal,  
    TimeSpan NotifyTime,
    string Timezone
);