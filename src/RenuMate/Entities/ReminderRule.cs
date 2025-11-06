using RenuMate.Events;

namespace RenuMate.Entities;

public class ReminderRule : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    
    public Subscription Subscription { get; set; } = null!;

    private TimeSpan _notifyTimeUtc;
    public TimeSpan NotifyTimeUtc
    {
        get => _notifyTimeUtc;
        set
        {
            if (_notifyTimeUtc == value)
            {
                return;
            }
            
            _notifyTimeUtc = value;
            AddDomainEvent(new ReminderRuleUpdatedEvent(Id, DaysBeforeRenewal, NotifyTimeUtc));
        }
    }

    private int _daysBeforeRenewal;
    public int DaysBeforeRenewal
    {
        get => _daysBeforeRenewal;
        set
        {
            if (_daysBeforeRenewal == value)
            {
                return;
            }
            
            _daysBeforeRenewal = value;
            AddDomainEvent(new ReminderRuleUpdatedEvent(Id, DaysBeforeRenewal, NotifyTimeUtc));
        }
    }
    
    public ICollection<ReminderOccurrence> ReminderOccurrences { get; set; }
}