using RenuMate.Enums;
using RenuMate.Events;

namespace RenuMate.Entities;

public class Subscription : BaseEntity
{
    public string Name { get; set; } = null!;
    
    public SubscriptionPlan Plan { get; set; }
    
    public int? CustomPeriodInDays { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime RenewalDate { get; private set; }
    
    public decimal Cost { get; set; }

    public Currency Currency { get; set; }

    public bool IsMuted { get; set; }
    
    public string? Note { get; set; }
    
    public string? CancelLink { get; set; }
    
    public string? PicLink { get; set; }

    public Guid UserId { get; set; }

    public User User { get; init; } = null!;

    public ICollection<ReminderRule> Reminders { get; set; } = [];

    public void UpdateNextRenewalDate()
    {
        var today = DateTime.UtcNow.Date;
        
        var next = RenewalDate != default ? RenewalDate : StartDate;

        if (next > today)
        {
            return;
        }

        while (next <= today)
        {
            next = Plan switch
            {
                SubscriptionPlan.Monthly => next.AddMonths(1),
                SubscriptionPlan.Quarterly => next.AddMonths(3),
                SubscriptionPlan.Annual => next.AddYears(1),
                SubscriptionPlan.Custom => CustomPeriodInDays.HasValue 
                    ? next.AddDays(CustomPeriodInDays.Value) 
                    : throw new InvalidOperationException("Custom period requires days to be specified."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (RenewalDate == next)
        {
            return;
        }
        
        RenewalDate = next;
        AddDomainEvent(new SubscriptionRenewalDateUpdatedEvent(Id, RenewalDate));
    }
}