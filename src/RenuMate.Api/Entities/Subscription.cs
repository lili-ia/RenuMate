using RenuMate.Api.Enums;
using RenuMate.Api.Events;
using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Entities;

public class Subscription : BaseEntity
{
    public string Name { get; private set; } = null!;
    
    public SubscriptionPlan Plan { get; private set; }
    
    public int? CustomPeriodInDays { get; private set; }
    
    public int? TrialPeriodInDays { get; private set; }
    
    public DateOnly StartDate { get; private set; }
    
    public DateOnly RenewalDate { get; private set; }
    
    public decimal Cost { get; private set; }
    
    public Currency Currency { get; private set; }
    
    public bool IsMuted { get; private set; }
    
    public string? Note { get; private set; }
    
    public string? CancelLink { get; private set; }
    
    public string? PicLink { get; private set; }
    
    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;
    
    public IReadOnlyCollection<ReminderRule> Reminders => _reminders.AsReadOnly();
    
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    
    public static Subscription CreateTrial(
        string name, 
        int trialDays, 
        DateOnly startDate,
        Guid userId,
        decimal postTrialCost,
        Currency currency,
        DateOnly today,
        string? cancelLink = null,
        string? picLink = null, 
        string? note = null)
    {
        if (trialDays <= 0)
        {
            throw new DomainValidationException("Trial period must be positive.");
        }
        
        var trialEnd = startDate.AddDays(trialDays);

        if (trialEnd <= today)
        {
            throw new DomainValidationException("Cannot create a trial that has already ended.");
        }
        
        var initialRenewalDate = CalculateInitialRenewal(SubscriptionPlan.Trial, startDate, today, trialDays, null);

        return new Subscription(
            name, SubscriptionPlan.Trial, startDate, initialRenewalDate,
            postTrialCost, currency, userId, trialDays: trialDays,
            note: note, cancelLink: cancelLink, picLink: picLink);
    }

    public static Subscription CreateStandard(
        string name, 
        SubscriptionPlan plan, 
        decimal cost, 
        Currency currency, 
        DateOnly startDate,
        Guid userId,
        DateOnly today,
        string? cancelLink = null,
        string? picLink = null, 
        string? note = null)
    {
        if (plan is SubscriptionPlan.Custom or SubscriptionPlan.Trial)
        {
            throw new DomainValidationException("Use specific factory methods for Custom or Trial plans.");
        }
        
        var renewalDate = CalculateInitialRenewal(
            plan, startDate, today, null, null);

        return new Subscription(name, plan, startDate, renewalDate, cost, currency, userId, 
            note: note, cancelLink: cancelLink, picLink: picLink);
    }

    public static Subscription CreateCustom(
        string name, 
        int customPeriodInDays, 
        decimal cost, 
        Currency currency, 
        DateOnly startDate,
        Guid userId,
        DateOnly now,
        string? cancelLink = null,
        string? picLink = null, 
        string? note = null)
    {
        if (customPeriodInDays <= 0)
        {
            throw new DomainValidationException("Custom period must be positive.");
        }
        
        var renewalDate = CalculateInitialRenewal(
            SubscriptionPlan.Custom, startDate, now, null, customPeriodInDays);

        return new Subscription(
            name, SubscriptionPlan.Custom, startDate, renewalDate,
            cost, currency, userId, customDays: customPeriodInDays,
            note: note, cancelLink: cancelLink, picLink: picLink);
    }

    public void SetMuteStatus(bool status)
    {
        IsMuted = status;
    }

    public void UpdateNextRenewalDate(DateOnly today)
    {
        var next = RenewalDate != default ? RenewalDate : StartDate;

        while (next <= today)
        {
            if (Plan == SubscriptionPlan.Trial)
            {
                Plan = SubscriptionPlan.Monthly; 
                next = AddPeriod(Plan, next);
            }
            else
            {
                next = AddPeriod(Plan, next, CustomPeriodInDays);
            }
        }

        if (RenewalDate != next)
        {
            RenewalDate = next;
            AddDomainEvent(new SubscriptionRenewalDateOrPlanUpdatedEvent(Id, RenewalDate));
        }
    }
    
    public void UpdatePlanAndStartDate(
        SubscriptionPlan newPlan,
        DateOnly newStartDate,
        DateOnly today,
        int? customDays = null,
        int? trialDays = null)
    {
        var changed = false;

        if (StartDate != newStartDate)
        {
            StartDate = newStartDate;
            changed = true;
        }

        if (Plan != newPlan || CustomPeriodInDays != customDays || TrialPeriodInDays != trialDays)
        {
            Plan = newPlan;
            CustomPeriodInDays = customDays;
            TrialPeriodInDays = trialDays;
            changed = true;
        }

        if (changed)
        {
            RenewalDate = CalculateInitialRenewal(Plan, StartDate, today, TrialPeriodInDays, CustomPeriodInDays);
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
    
    public void AddReminderRule(TimeSpan notifyTimeUtc, int daysBeforeRenewal, DateTime now)
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

        var occurrence = rule.CreateOccurrence(RenewalDate, now);
        
        if (occurrence is not null)
        {
            rule.AddOccurrence(occurrence);
        }
    }

    public void ClearAllReminderRules()
    {
        foreach (var rule in _reminders.ToList())
        {
            rule.ClearUnsentReminderOccurrences();
        }
        
        _reminders.Clear();
    }
    
    public void AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Id == tag.Id))
        {
            return;
        }
        
        _tags.Add(tag);
    }
    
    public void UpdateTags(List<Tag> newTags)
    {
        _tags.Clear();

        foreach (var tag in newTags)
        {
            _tags.Add(tag);
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
    
    private static DateOnly AddPeriod(SubscriptionPlan plan, DateOnly current, int? customDays = null) => plan switch
    {
        SubscriptionPlan.Monthly => current.AddMonths(1),
        SubscriptionPlan.Quarterly => current.AddMonths(3),
        SubscriptionPlan.Annual => current.AddYears(1),
        SubscriptionPlan.Custom => current.AddDays(customDays ?? 30),
        _ => current.AddMonths(1)
    };
    
    private static DateOnly CalculateInitialRenewal(
        SubscriptionPlan plan, 
        DateOnly startDate, 
        DateOnly today, 
        int? trialDays, 
        int? customDays) 
    {
        var next = startDate;

        if (plan == SubscriptionPlan.Trial)
        {
            next = startDate.AddDays(trialDays ?? 7);
        }
        else
        {
            while (next <= today) 
            {
                next = AddPeriod(plan, next, customDays);
            }
        }

        return next;
    }
    
    private Subscription() { }
    
    private  Subscription(
        string name, 
        SubscriptionPlan plan, 
        DateOnly startDate, 
        DateOnly renewalDate,
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

        if (startDate < DateOnly.FromDayNumber(719162))
        {
            throw new DomainValidationException("Invalid start date.");
        }

        Name = name;
        Plan = plan;
        StartDate = startDate;
        RenewalDate = renewalDate;
        Cost = cost;
        Currency = currency;
        UserId = userId;
        TrialPeriodInDays = trialDays;
        CustomPeriodInDays = customDays;
        Note = note;
        CancelLink = cancelLink;
        PicLink = picLink;
        IsMuted = false;
    }
    
    private readonly List<ReminderRule> _reminders = [];
    
    private readonly List<Tag> _tags = [];
}