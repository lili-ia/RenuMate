using RenuMate.Events;

namespace RenuMate.Entities;

public class ReminderRule : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    
    public Subscription Subscription { get; set; } = null!;

    private TimeSpan _notifyTime;
    public TimeSpan NotifyTime
    {
        get => _notifyTime;
        set
        {
            if (_notifyTime == value)
            {
                return;
            }
            
            _notifyTime = value;
            AddDomainEvent(new ReminderRuleUpdatedEvent(Id, DaysBeforeRenewal, NotifyTime));
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
            AddDomainEvent(new ReminderRuleUpdatedEvent(Id, DaysBeforeRenewal, NotifyTime));
        }
    }
    
    public ICollection<ReminderOccurrence> ReminderOccurrences { get; set; }
}