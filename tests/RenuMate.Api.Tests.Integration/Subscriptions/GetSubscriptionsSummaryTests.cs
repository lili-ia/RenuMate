using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RenuMate.Api.DTOs;
using RenuMate.Api.Enums;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Tests.Integration.Subscriptions;

public class GetSubscriptionsSummaryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Dictionary<Currency, decimal> _fakeRates = new Dictionary<Currency, decimal>
    {
        { Currency.EUR, 1m },
        { Currency.USD, 2m },
        { Currency.GBP, 4m }
    };
    
    [Fact]
    public async Task Handle_InvalidCurrency_ReturnsBadRequest()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.GetAsync("/api/subscriptions/summary?currency=INVALID&period=monthly");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task Handle_NoSubscriptions_ReturnsZeroSummary()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        
        // Act
        var response = await client.GetFromJsonAsync<SubscriptionSummaryDto>(
            "/api/subscriptions/summary?currency=EUR&period=monthly", options);

        // Assert
        response!.TotalCost.Should().Be(0);
        response.ProjectedCost.Should().Be(0);
        response.ActiveSubscriptionsCount.Should().Be(0);
        response.ActiveRemindersCount.Should().Be(0);
    }
    
    [Fact] 
    public async Task Handle_MultipleSubscriptionsAndPeriods_ReturnsCorrectTotals()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.CurrencyServiceMock.Invocations.Clear();
        
        await factory.CreateExampleUserAsync(_userId);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, 
            plan: SubscriptionPlan.Monthly, cost: 30);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, 
            plan: SubscriptionPlan.Trial, cost: 15);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow,
            plan: SubscriptionPlan.Annual, cost: 365);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        factory.CurrencyServiceMock
            .Setup(x => x.GetRateForDesiredCurrency(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fakeRates);
        
        // Act
        var monthly = await client.GetFromJsonAsync<SubscriptionSummaryDto>(
            "/api/subscriptions/summary?currency=EUR&period=monthly", options);
        var daily = await client.GetFromJsonAsync<SubscriptionSummaryDto>(
            "/api/subscriptions/summary?currency=EUR&period=daily", options);
        
        // Assert
        monthly!.ActiveSubscriptionsCount.Should().Be(3);
        monthly.TotalCost.Should().BeApproximately(30 + 30, 0.1m);
        monthly.ProjectedCost.Should().BeGreaterThan(monthly.TotalCost);
        
        daily!.TotalCost.Should().BeApproximately(30/30m + 30/30m, 0.1m);
    }
    
    [Fact]
    public async Task Handle_MutedSubscription_IsIgnored()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow,
            plan: SubscriptionPlan.Monthly, cost: 30, isMuted: true);
        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, 
            plan: SubscriptionPlan.Monthly, cost: 20);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
        
        // Act
        var summary = await client.GetFromJsonAsync<SubscriptionSummaryDto>(
            "/api/subscriptions/summary?currency=EUR&period=monthly", options);

        // Assert
        summary!.ActiveSubscriptionsCount.Should().Be(1);
        summary.TotalCost.Should().BeApproximately(20, 0.1m);
    }
    
    
    [Fact]
    public async Task Handle_CountsActiveRemindersOnlyForNonMutedSubscriptions()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var subId = Guid.NewGuid();
        await factory.CreateExampleSubscriptionAsync(subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRule(Guid.NewGuid(), Guid.NewGuid(), subId);

        var mutedSubId = Guid.NewGuid();
        await factory.CreateExampleSubscriptionAsync(mutedSubId, _userId, DateTime.UtcNow, isMuted: true);
        await factory.CreateExampleReminderRule(Guid.NewGuid(), Guid.NewGuid(), mutedSubId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
        
        // Act
        var summary = await client.GetFromJsonAsync<SubscriptionSummaryDto>("/api/subscriptions/summary?currency=EUR&period=monthly");

        // Assert
        summary!.ActiveRemindersCount.Should().Be(1);
    }
    
    [Fact]
    public async Task Handle_NoChanges_ReturnsCachedCurrencies()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.CurrencyServiceMock.Invocations.Clear();
        await factory.CreateExampleUserAsync(_userId);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, cost: 30, isMuted: true);
        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, cost: 20);
        
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
        
        factory.CurrencyServiceMock
            .Setup(x => x.GetRateForDesiredCurrency(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fakeRates);

        // Act
        var first = await client.GetAsync("/api/subscriptions/summary?currency=USD&period=monthly");
        var second = await client.GetAsync("/api/subscriptions/summary?currency=USD&period=monthly");

        // Assert
        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        // first request creates cache record, second one hits the cache, so currency service must be called exactly ONCE
        factory.CurrencyServiceMock.Verify( 
            x => x.GetRateForDesiredCurrency(
                It.IsAny<Currency>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentCurrencies_CalculatesTotalsWithCurrencyConversion()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.CurrencyServiceMock.Invocations.Clear();
        
        var userId = Guid.NewGuid();
        await factory.CreateExampleUserAsync(userId);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), userId, DateTime.UtcNow, cost: 30, currency: Currency.USD); 
        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), userId, DateTime.UtcNow, cost: 20, currency: Currency.EUR);
        // should be ignored bc muted
        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), userId, DateTime.UtcNow, cost: 10, currency: Currency.GBP, isMuted: true); 

        factory.CurrencyServiceMock
            .Setup(x => x.GetRateForDesiredCurrency(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fakeRates);

        var client = factory.GetAuthenticatedClient(userId: userId.ToString());

        // Act
        var summary = await client.GetFromJsonAsync<SubscriptionSummaryDto>(
            "/api/subscriptions/summary?currency=USD&period=monthly");

        // Assert
        summary.Should().NotBeNull();

        summary.TotalCost.Should().BeApproximately(35m, 0.01m);
        summary.ProjectedCost.Should().BeApproximately(35m, 0.01m);

        summary.ActiveSubscriptionsCount.Should().Be(2);
        summary.ActiveRemindersCount.Should().Be(0); 
    }

    [Fact]
    public async Task Handle_CurrencyServiceUnavailable_ReturnsServiceUnavailable()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.CurrencyServiceMock.Invocations.Clear();
        
        var userId = Guid.NewGuid();
        await factory.CreateExampleUserAsync(userId);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), userId, DateTime.UtcNow, cost: 30); 
        
        factory.CurrencyServiceMock
            .Setup(x => x.GetRateForDesiredCurrency(It.IsAny<Currency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dictionary<Currency, decimal>?)null);

        var client = factory.GetAuthenticatedClient(userId: userId.ToString());

        // Act
        var response = await client.GetAsync("/api/subscriptions/summary?currency=USD&period=monthly");

        // Assert

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}