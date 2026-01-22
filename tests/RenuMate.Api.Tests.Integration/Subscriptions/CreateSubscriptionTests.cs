using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Enums;
using RenuMate.Api.Persistence;
using RenuMate.Api.Subscriptions.Create;

namespace RenuMate.Api.Tests.Integration.Subscriptions;

public class CreateSubscriptionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    
    [Fact]
    public async Task Handle_StandardPlan_ShouldPersistAndReturnSubscription()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var subscription = await db.Subscriptions.SingleAsync(s => s.Name == "Netflix");
        subscription.Plan.Should().Be(SubscriptionPlan.Monthly);
    }

    [Fact]
    public async Task Handle_TrialPlan_UsesTrialPeriodAndHasCorrectRenewalDate()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest(plan: SubscriptionPlan.Trial, trialPeriodInDays: 14);

        // Act
        var response = await client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var subscription = await db.Subscriptions.SingleAsync();
        subscription.RenewalDate.Should().Be(subscription.StartDate.AddDays(14));
    }

    [Fact]
    public async Task Handle_CustomPlan_UsesCustomPeriodAndHasCorrectRenewalDate()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest(plan: SubscriptionPlan.Custom, customPeriodInDays: 45);

        // Act
        var response = await client.PostAsJsonAsync("/api/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var subscription = await db.Subscriptions.SingleAsync();
        subscription.RenewalDate.Should().Be(subscription.StartDate.AddDays(45));
    }
    
    [Fact]
    public async Task Handle_DuplicateName_ShouldReturnForbidden()
    {
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest("Netflix");

        await client.PostAsJsonAsync("/api/subscriptions", request);
        var response = await client.PostAsJsonAsync("/api/subscriptions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        (await db.Subscriptions.CountAsync()).Should().Be(1);
    }

    private static CreateSubscriptionRequest CreateValidRequest(
        string name = "Netflix", 
        SubscriptionPlan plan = SubscriptionPlan.Monthly, 
        int? customPeriodInDays = 0,
        int? trialPeriodInDays = 0)
    {
        return new CreateSubscriptionRequest(
            Name: name,
            Plan: plan.ToString(),
            CustomPeriodInDays: customPeriodInDays,
            TrialPeriodInDays: trialPeriodInDays,
            StartDate: DateTime.UtcNow.Date,
            Cost: 18,
            Currency: "USD",
            Note: null,
            CancelLink: null,
            PicLink: null
        );
    }
}