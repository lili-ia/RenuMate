using FluentAssertions;
using RenuMate.Api.Entities;
using RenuMate.Api.Enums;
using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Tests.Unit.Entities;

public class SubscriptionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateTrial_TrialDaysIsZeroOrNegative_ThrowsDomainValidationException(int invalidTrialDays)
    {
        // Act
        var now = DateTime.UtcNow;
        
        var action = () => Subscription.CreateTrial(
            name: "Netflix", 
            trialDays: invalidTrialDays, 
            startDate: now,
            userId: Guid.NewGuid(), 
            postTrialCost: 15, 
            Currency.EUR, now: now);
        
        // Assert
        action.Should().Throw<DomainValidationException>().WithMessage("Trial period must be positive.");
    }
    
    [Theory]
    [InlineData(SubscriptionPlan.Custom)]
    [InlineData(SubscriptionPlan.Trial)]
    public void CreateStandard_UsingTrialOrCustomPlan_ThrowsDomainValidationException(SubscriptionPlan plan)
    {
        // Act
        var now = DateTime.UtcNow;

        var action = () => Subscription.CreateStandard(
            name: "Netflix", 
            plan, 
            cost: 15, 
            Currency.EUR,
            startDate: now,
            userId: Guid.NewGuid(), 
            now: now);
        
        // Assert
        action.Should().Throw<DomainValidationException>().WithMessage("Use specific factory methods for Custom or Trial plans.");
    }
    
    [Theory]
    [InlineData("2026-01-01", 7, "2026-01-08")] 
    [InlineData("2026-01-01", 1, "2026-01-02")] 
    [InlineData("2026-01-01", 30, "2026-01-31")] 
    [InlineData("2025-12-25", 14, "2026-01-08")] 
    [InlineData("2024-02-28", 2, "2024-03-01")]
    public void UpdateNextRenewalDate_TrialPlan_SetsRenewalDateToStartDatePlusTrialDays(
        string startDateStr, 
        int trialDays, 
        string expectedRenewalStr)
    {
        // Arrange
        var startDate = DateTime.Parse(startDateStr);
        var expectedRenewal = DateTime.Parse(expectedRenewalStr);
    
        var sub = Subscription.CreateTrial(
            name: "Netflix", 
            trialDays: trialDays, 
            startDate: startDate, 
            userId: Guid.NewGuid(), 
            postTrialCost: 15, 
            currency: Currency.EUR, 
            now: startDate);

        // Act
        sub.UpdateNextRenewalDate(startDate);
    
        // Assert
        sub.RenewalDate.Should().Be(expectedRenewal);
        sub.Plan.Should().Be(SubscriptionPlan.Trial); 
    }
    
    [Theory]
    [InlineData("2026-01-01", 7, "2026-01-08", "2026-02-08")] 
    [InlineData("2026-01-10", 21, "2026-01-31", "2026-02-28")]
    [InlineData("2024-02-01", 27, "2024-02-28", "2024-03-28")]
    [InlineData("2026-05-20", 1, "2026-05-21", "2026-06-21")]
    public void UpdateNextRenewalDate_TrialEndsToday_MovesToMonthlyAndSetsNextPeriod(
        string startDateStr, 
        int trialDays, 
        string checkDateStr, 
        string expectedRenewalStr)
    {
        // Arrange
        var startDate = DateTime.Parse(startDateStr);
        var checkDate = DateTime.Parse(checkDateStr); 
        var expectedRenewal = DateTime.Parse(expectedRenewalStr);

        var sub = Subscription.CreateTrial(
            name: "Netflix", 
            trialDays: trialDays, 
            startDate: startDate, 
            userId: Guid.NewGuid(), 
            postTrialCost: 15, 
            currency: Currency.EUR, 
            now: startDate);

        // Act
        sub.UpdateNextRenewalDate(checkDate);

        // Assert
        sub.Plan.Should().Be(SubscriptionPlan.Monthly);
        sub.RenewalDate.Should().Be(expectedRenewal);
    }
    
    [Fact]
    public void UpdateNextRenewalDate_SubscriptionIsOverdue_CalculatesNextFutureDate()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var now = new DateTime(2025, 2, 15); 
        var sub = Subscription.CreateStandard(
            name: "Netflix", 
            SubscriptionPlan.Monthly, 
            cost: 15, 
            Currency.EUR, 
            startDate, 
            userId: Guid.NewGuid(),
            now: now);

        // Act
        sub.UpdateNextRenewalDate(now);

        // Assert
        sub.RenewalDate.Should().Be(new DateTime(2025, 3, 1));
    }
    
    [Fact]
    public void UpdateNextRenewalDate_ExpiredTrial_ConvertsToMonthlyAndSetsNextRenewal()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var trialStart = now;
        var trialDays = 7;
        var userId = Guid.NewGuid();

        var sub = Subscription.CreateTrial(
            name: "Netflix",
            trialDays: trialDays,
            startDate: trialStart,
            userId: userId,
            postTrialCost: 15,
            currency: Currency.EUR,
            now: now
        );

        sub.Plan.Should().Be(SubscriptionPlan.Trial);
        sub.RenewalDate.Should().Be(trialStart.AddDays(trialDays));
        
        var future = now.AddDays(14);
        
        // Act
        sub.UpdateNextRenewalDate(future);

        // Assert
        sub.Plan.Should().Be(SubscriptionPlan.Monthly); 
        sub.RenewalDate.Should().BeAfter(future);         
    }
    
    [Fact]
    public void UpdateNextRenewalDate_DateIsInFuture_DoesNotChangeRenewalDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var futureDate = now.AddDays(10).Date;

        var sub = Subscription.CreateStandard("Netflix", SubscriptionPlan.Monthly, 15, Currency.USD, 
            futureDate, Guid.NewGuid(), futureDate);
        var initialRenewalDate = sub.RenewalDate;

        // Act
        sub.UpdateNextRenewalDate(now);

        // Assert
        sub.RenewalDate.Should().Be(initialRenewalDate);
    }

    [Fact]
    public void UpdateNextRenewalDate_SubscriptionIsWayOverdue_CalculatesNextFutureDate()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-3).Date;
        var now = DateTime.UtcNow;
        
        var sub = Subscription.CreateStandard("Spotify", SubscriptionPlan.Monthly, 10, Currency.USD, 
            startDate, Guid.NewGuid(), now, null, null, null);
        
        // Act
        sub.UpdateNextRenewalDate(now);

        // Assert
        sub.RenewalDate.Should().BeAfter(DateTime.UtcNow);
        sub.RenewalDate.Should().Be(startDate.AddMonths(4)); 
    }

    [Fact]
    public void UpdateNextRenewalDate_NewlyCreatedSubscription_SetsFirstValidFutureDate()
    {
        // Arrange
        var now = new DateTime(2026, 1, 18);
        var startDate = new DateTime(2025, 12, 1);
    
        var sub = Subscription.CreateStandard("Netflix", SubscriptionPlan.Monthly, 15, Currency.EUR, 
            startDate, Guid.NewGuid(), now, null, null, null);

        // Act
        sub.UpdateNextRenewalDate(now);

        // Assert
        sub.RenewalDate.Should().Be(new DateTime(2026, 2, 1));
    }

    [Fact]
    public void UpdateNextRenewalDate_ExistingSubscription_MovesToNextPeriodOnlyIfOverdue()
    {
        // Arrange
        var now = new DateTime(2026, 1, 18);
        var futureRenewal = new DateTime(2026, 1, 25); 
    
        var sub = Subscription.CreateStandard("Spotify", SubscriptionPlan.Monthly, 10, Currency.USD, 
            new DateTime(2025, 12, 25), Guid.NewGuid(), now);

        // Act 1
        sub.UpdateNextRenewalDate(now);
        // Assert 1
        sub.RenewalDate.Should().Be(futureRenewal);

        // Act 2
        var futureNow = new DateTime(2026, 1, 26);
        sub.UpdateNextRenewalDate(futureNow);

        // Assert 2
        sub.RenewalDate.Should().Be(new DateTime(2026, 2, 25));
    }
    
    [Fact]
    public void AddReminderRule_LimitExceeded_ThrowsDomainConflictException()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        var sub = Subscription.CreateStandard("Netflix", SubscriptionPlan.Annual, 100, Currency.EUR, 
            now.AddDays(1), Guid.NewGuid(), now);
    
        sub.AddReminderRule(TimeSpan.FromHours(10), 1, now);
        sub.AddReminderRule(TimeSpan.FromHours(12), 2, now);
        sub.AddReminderRule(TimeSpan.FromHours(14), 3, now);

        // Act
        var action = () => sub.AddReminderRule(TimeSpan.FromHours(16), 4, now);

        // Assert
        action.Should().Throw<DomainConflictException>()
            .WithMessage("Maximum 3 reminders allowed per subscription.");
    }

    [Fact]
    public void AddReminderRule_DuplicateRule_ThrowsDomainConflictException()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        var sub = Subscription.CreateStandard("Netflix", SubscriptionPlan.Monthly, 15, Currency.EUR, 
            now.AddDays(1), Guid.NewGuid(), now, null, null, null);
        var time = TimeSpan.FromHours(10);
        sub.AddReminderRule(time, 1, now);

        // Act
        var action = () => sub.AddReminderRule(time, 1, now);

        // Assert
        action.Should().Throw<DomainConflictException>()
            .WithMessage("A similar reminder rule already exists.");
    }

    [Fact]
    public void AddReminderRule_DaysBeforeGreaterThenPlanDuration_ThrowsDomainValidationException()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var sub = Subscription.CreateStandard("Netflix", SubscriptionPlan.Monthly, 15, Currency.EUR, 
            now.AddDays(1), Guid.NewGuid(), now);

        // Act
        var action = () => sub.AddReminderRule(TimeSpan.FromHours(10), 30, now);

        // Assert
        action.Should().Throw<DomainValidationException>()
            .WithMessage("*Maximum allowed for your plan is 27 days*");
    }
    
    [Fact]
    public void UpdatePlanAndStartDate_SubscriptionAlreadyStarted_UpdatesValuesAndRenewalDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var startDate = now.AddDays(-5);
        var sub = Subscription.CreateStandard(
            name: "Netflix",
            plan: SubscriptionPlan.Monthly,
            cost: 15,
            currency: Currency.EUR,
            startDate: startDate,
            userId: Guid.NewGuid(),
            now: startDate.AddDays(-1)); 

        var newStartDate = startDate.AddDays(2); 
        var newPlan = SubscriptionPlan.Quarterly;

        // Act
        sub.UpdatePlanAndStartDate(newPlan, newStartDate, now);

        // Assert
        sub.StartDate.Should().Be(newStartDate);
        sub.Plan.Should().Be(newPlan);
        sub.RenewalDate.Should().BeAfter(now); 
    }
    
    [Fact]
    public void ChangePricing_NegativeCost_ThrowsDomainValidationException()
    {
        // Arrange
        var sub = Subscription.CreateStandard("Netflix", SubscriptionPlan.Monthly, 15, Currency.EUR, 
            DateTime.UtcNow.AddDays(1), Guid.NewGuid(), DateTime.UtcNow);

        // Act
        var action = () => sub.ChangePricing(-1, Currency.EUR);

        // Assert
        action.Should().Throw<DomainValidationException>()
            .WithMessage("Cost cannot be negative.");
    }
}