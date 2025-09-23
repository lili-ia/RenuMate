using RenuMate.Enums;
using RenuMate.Events;
using RenuMate.Extensions;

namespace RenuMate.Entities;

public class Subscription : BaseEntity
{
    public string Name { get; set; } = null!;
    
    public SubscriptionPlan Plan { get; set; }
    
    public int? CustomPeriodInDays { get; set; }
    
    public DateTime StartDate { get; set; }

    private DateTime _renewalDate;
    public DateTime RenewalDate
    {
        get => _renewalDate;
        set
        {
            if (_renewalDate == value)
            {
                return;
            }
            
            _renewalDate = value;
            AddDomainEvent(new SubscriptionRenewalDateUpdatedEvent(Id, RenewalDate));
        }
    }
    
    public decimal Cost { get; set; }

    public Currency Currency { get; set; }

    public bool IsMuted { get; set; }
    
    public string? Note { get; set; }
    
    public string? CancelLink { get; set; }
    
    public string? PicLink { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
    
    public ICollection<ReminderRule> Reminders { get; set; }
}