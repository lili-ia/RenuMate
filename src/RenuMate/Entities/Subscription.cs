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
    
    public DateTime RenewalDate { get; set; }
    
    public decimal Cost { get; set; }

    public Currency Currency { get; set; }
    
    public string? Note { get; set; }
    
    public string? CancelLink { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
    
    public ICollection<Reminder> Reminders { get; set; }


    private readonly List<object> _domainEvents = [];

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(object eventItem) => _domainEvents.Add(eventItem);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void Renew()
    {
        RenewalDate = RenewalDate.AddPeriod(Plan, CustomPeriodInDays);

        AddDomainEvent(new SubscriptionRenewedEvent(this));
    }
}