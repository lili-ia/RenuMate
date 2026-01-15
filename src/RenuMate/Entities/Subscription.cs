using RenuMate.Enums;
using RenuMate.Events;
using RenuMate.Exceptions;
using SendGrid.Helpers.Errors.Model;

namespace RenuMate.Entities;

public class Subscription : BaseEntity
{
    public string Name { get; private set; } = null!;
    
    public SubscriptionPlan Plan { get; private set; }
    
    public int? CustomPeriodInDays { get; private set; }
    
    public int? TrialPeriodInDays { get; private set; }
    
    public DateTime StartDate { get; private set; }
    
    public DateTime RenewalDate { get; private set; }
    
    public decimal Cost { get; private set; }
    
    public Currency Currency { get; private set; }
    
    public bool IsMuted { get; private set; }
    
    public string? Note { get; private set; }
    
    public string? CancelLink { get; private set; }
    
    public string? PicLink { get; private set; }
    
    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;
    
    public IReadOnlyCollection<ReminderRule> Reminders => _reminders.AsReadOnly();
    
    public static Subscription CreateTrial(
        string name, 
        int trialDays, 
        Guid userId,
        decimal postTrialCost,
        Currency currency,
        string? cancelLink,
        string? picLink, 
        string? note)
    {
        if (trialDays <= 0)
        {
            throw new DomainValidationException("Trial period must be positive.");
        }

        return new Subscription(
            name, SubscriptionPlan.Trial, DateTime.UtcNow, 
            postTrialCost, currency, userId, trialDays: trialDays,
            note: note, cancelLink: cancelLink, picLink: picLink);
    }

    public static Subscription CreateStandard(
        string name, 
        SubscriptionPlan plan, 
        decimal cost, 
        Currency currency, 
        DateTime startDate,
        Guid userId,
        string? cancelLink,
        string? picLink, 
        string? note)
    {
        if (plan is SubscriptionPlan.Custom or SubscriptionPlan.Trial)
        {
            throw new DomainValidationException("Use specific factory methods for Custom or Trial plans.");
        }

        return new Subscription(name, plan, startDate, cost, currency, userId, 
            note: note, cancelLink: cancelLink, picLink: picLink);
    }

    public static Subscription CreateCustom(
        string name, 
        int customPeriodInDays, 
        decimal cost, 
        Currency currency, 
        DateTime startDate,
        Guid userId,
        string? cancelLink,
        string? picLink, 
        string? note)
    {
        if (customPeriodInDays <= 0)
        {
            throw new DomainValidationException("Custom period must be positive.");
        }

        return new Subscription(
            name, SubscriptionPlan.Custom, startDate, 
            cost, currency, userId, customDays: customPeriodInDays,
            note: note, cancelLink: cancelLink, picLink: picLink);
    }

    public void SetMuteStatus(bool status)
    {
        IsMuted = status;
    }

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
                while (next <= today) next = AddPeriod(next);
                RenewalDate = next;
            }
            return;
        }

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
            AddDomainEvent(new SubscriptionRenewalDateOrPlanUpdatedEvent(Id, RenewalDate));
        }
    }
    
    public void UpdateDetails(string name, string? note, string? cancelLink, string? picLink)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Name cannot be empty.");
    
        Name = name;
        Note = note;
        CancelLink = cancelLink;
        PicLink = picLink;
    }

    public void ChangePricing(decimal newCost, Currency newCurrency)
    {
        if (newCost < 0)
        {
            throw new DomainValidationException("Cost cannot be negative.");
        }
    
        Cost = newCost;
        Currency = newCurrency;
    }

    public void ChangePlan(SubscriptionPlan newPlan, int? customDays = null)
    {
        Plan = newPlan;
        CustomPeriodInDays = customDays;
        
        UpdateNextRenewalDate(); 
    }
    
    public void RescheduleStartDate(DateTime newDate)
    {
        if (StartDate <= DateTime.UtcNow)
        {
            throw new DomainValidationException("Cannot change start date for an active or past subscription.");
        }
        
        StartDate = newDate;
        UpdateNextRenewalDate(); 
    }
    
    public void AddReminderRule(TimeSpan notifyTimeUtc, int daysBeforeRenewal)
    {
        if (_reminders.Count >= 3)
        {
            throw new DomainConflictException("Maximum 3 reminders allowed per subscription.");
        }

        if (_reminders.Any(r => r.DaysBeforeRenewal == daysBeforeRenewal && r.NotifyTimeUtc == notifyTimeUtc))
        {
            throw new DomainConflictException("A similar reminder rule already exists.");
        }
        
        var maxDays = GetPlanDurationInDays();
        if (daysBeforeRenewal >= maxDays)
        {
            throw new DomainValidationException(
                $"Reminder cannot be set for {daysBeforeRenewal} days before. " +
                $"Maximum allowed for your plan is {maxDays - 1} days.");
        }
        
        var rule = ReminderRule.Create(Id, notifyTimeUtc, daysBeforeRenewal);
        _reminders.Add(rule);

        var occurrence = rule.CreateOccurrence(RenewalDate);
        
        if (occurrence is not null)
        {
            rule.AddOccurrence(occurrence);
        }
    }
    
    private int GetPlanDurationInDays()
    {
        return Plan switch
        {
            SubscriptionPlan.Monthly => 28, 
            SubscriptionPlan.Quarterly => 90,
            SubscriptionPlan.Annual => 365,
            SubscriptionPlan.Custom => CustomPeriodInDays ?? 1,
            SubscriptionPlan.Trial => TrialPeriodInDays ?? 1,
            _ => 0
        };
    }

    private DateTime AddPeriod(DateTime current) => Plan switch
    {
        SubscriptionPlan.Monthly => current.AddMonths(1),
        SubscriptionPlan.Quarterly => current.AddMonths(3),
        SubscriptionPlan.Annual => current.AddYears(1),
        SubscriptionPlan.Custom => current.AddDays(CustomPeriodInDays ?? 30),
        _ => current.AddMonths(1)
    };
    
    private Subscription() { }
    
    private Subscription(
        string name, 
        SubscriptionPlan plan, 
        DateTime startDate, 
        decimal cost, 
        Currency currency, 
        Guid userId,
        int? trialDays = null,
        int? customDays = null,
        string? note = null,
        string? cancelLink = null,
        string? picLink = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Name is required.");
        }

        if (cost < 0)
        {
            throw new DomainValidationException("Cost cannot be negative.");
        }

        if (startDate < DateTime.UnixEpoch)
        {
            throw new DomainValidationException("Invalid start date.");
        }

        Name = name;
        Plan = plan;
        StartDate = startDate;
        Cost = cost;
        Currency = currency;
        UserId = userId;
        TrialPeriodInDays = trialDays;
        CustomPeriodInDays = customDays;
        Note = note;
        CancelLink = cancelLink;
        PicLink = picLink;
        IsMuted = false;

        UpdateNextRenewalDate(isInitialization: true);
    }
    
    private readonly List<ReminderRule> _reminders = [];
}