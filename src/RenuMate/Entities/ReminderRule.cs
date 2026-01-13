using RenuMate.Enums;

namespace RenuMate.Entities;

public class ReminderRule : BaseEntity
{
    public Guid SubscriptionId { get; init; }
    
    public Subscription Subscription { get; init; } = null!;

    public TimeSpan NotifyTimeUtc { get; init; }
    
    public int DaysBeforeRenewal { get; init; }

    public ICollection<ReminderOccurrence> ReminderOccurrences { get; init; } = [];
    
    public DateTime CalculateNextOccurrence(DateTime subscriptionRenewalDate, SubscriptionPlan plan, int? customDays)
    {
        var nextReminder = subscriptionRenewalDate.Date
            .AddDays(-DaysBeforeRenewal)
            .Add(NotifyTimeUtc);

        while (nextReminder <= DateTime.UtcNow)
        {
            nextReminder = plan switch
            {
                SubscriptionPlan.Monthly => nextReminder.AddMonths(1),
                SubscriptionPlan.Quarterly => nextReminder.AddMonths(3),
                SubscriptionPlan.Annual => nextReminder.AddYears(1),
                SubscriptionPlan.Custom when customDays.HasValue => nextReminder.AddDays(customDays.Value),
                _ => throw new InvalidOperationException("Unknown plan")
            };
        }
        return nextReminder;
    }
}