namespace RenuMate.Reminders.Create;

public record CreateReminderRequest(
    Guid SubscriptionId,
    TimeSpan NotifyTime,
    string Timezone,
    int DaysBeforeRenewal)
{
    public TimeSpan GetUtcNotifyTime()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Timezone);
        var baseDate = DateTime.Today.Add(NotifyTime);
        
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(baseDate, DateTimeKind.Unspecified), tz).TimeOfDay;
    }
}