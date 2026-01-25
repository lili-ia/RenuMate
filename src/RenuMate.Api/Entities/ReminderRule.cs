using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Entities;

public class ReminderRule : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    
    public Subscription Subscription { get; private set; } = null!;

    public TimeSpan NotifyTimeUtc { get; private set; }
    
    public int DaysBeforeRenewal { get; private set; }

    public IReadOnlyCollection<ReminderOccurrence> ReminderOccurrences => _reminderOccurrences.AsReadOnly();
    
    public static ReminderRule Create(Guid subscriptionId, TimeSpan notifyTimeUtc, int daysBeforeRenewal)
    {
        if (daysBeforeRenewal < 0)
        {
            throw new DomainValidationException("Days before renewal cannot be negative.");
        }
        
        return new ReminderRule
        {
            SubscriptionId = subscriptionId,
            NotifyTimeUtc = notifyTimeUtc,
            DaysBeforeRenewal = daysBeforeRenewal
        };
    }
    
    public ReminderOccurrence? CreateOccurrence(DateTime subscriptionRenewalDate, DateTime now)
    {
        var scheduledAt = subscriptionRenewalDate.Date
            .AddDays(-DaysBeforeRenewal)
            .Add(NotifyTimeUtc);

        if (scheduledAt <= now)
        {
            return null;
        }

        return ReminderOccurrence.Create(Id, scheduledAt, now);
    }
    
    public void AddOccurrence(ReminderOccurrence occurrence)
    {
        if (occurrence is null)
        {
            throw new ArgumentNullException(nameof(occurrence));
        }
        
        if (_reminderOccurrences.Any(o => o.ScheduledAt == occurrence.ScheduledAt))
        {
            throw new DomainConflictException(
                $"A reminder occurrence for this rule is already scheduled at {occurrence.ScheduledAt}.");
        }

        _reminderOccurrences.Add(occurrence);
    }

    public void ClearUnsentReminderOccurrences()
    {
        _reminderOccurrences.RemoveAll(o => !o.IsSent);
    }
    
    private ReminderRule() { }
    
    private readonly List<ReminderOccurrence> _reminderOccurrences = [];
}