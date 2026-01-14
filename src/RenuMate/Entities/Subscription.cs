using RenuMate.Enums;
using RenuMate.Events;

namespace RenuMate.Entities;

public class Subscription : BaseEntity
{
    public string Name { get; set; } = null!;
    
    public SubscriptionPlan Plan { get; set; }
    
    public int? CustomPeriodInDays { get; set; }
    
    public int? TrialPeriodInDays { get; set; }
    
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

    public void UpdateNextRenewalDate(bool isInitialization = false)
    {
        var today = DateTime.UtcNow.Date;
        var next = RenewalDate != default ? RenewalDate : StartDate;

        if (isInitialization)
        {
            if (Plan == SubscriptionPlan.Trial)
            {
                RenewalDate = StartDate.AddDays(TrialPeriodInDays ?? 7);
            }
            else
            {
                while (next <= today)
                {
                    next = AddPeriod(next);
                }
                RenewalDate = next;
            }
            return;
        }

        if (next > today) return;

        while (next <= today)
        {
            if (Plan == SubscriptionPlan.Trial)
            {
                next = next.AddDays(TrialPeriodInDays ?? 7);
                Plan = SubscriptionPlan.Monthly;
            }
            else
            {
                next = AddPeriod(next);
            }
        }

        if (RenewalDate != next)
        {
            RenewalDate = next;
            AddDomainEvent(new SubscriptionRenewalDateUpdatedEvent(Id, RenewalDate));
        }
    }

    private DateTime AddPeriod(DateTime current) => Plan switch
    {
        SubscriptionPlan.Monthly => current.AddMonths(1),
        SubscriptionPlan.Quarterly => current.AddMonths(3),
        SubscriptionPlan.Annual => current.AddYears(1),
        SubscriptionPlan.Custom => current.AddDays(CustomPeriodInDays ?? 30),
        _ => current.AddMonths(1)
    };
}