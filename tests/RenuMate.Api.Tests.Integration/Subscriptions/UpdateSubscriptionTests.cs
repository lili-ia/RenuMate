using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Enums;
using RenuMate.Api.Persistence;
using RenuMate.Api.Subscriptions.Update;

namespace RenuMate.Api.Tests.Integration.Subscriptions;

public class UpdateSubscriptionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    
    [Fact]
    public async Task Handle_ValidUpdate_ShouldModifySubscription()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest();

        // Act
        var response = await client.PutAsJsonAsync($"/api/subscriptions/{_subId}", request);

        // Assert
        response.EnsureSuccessStatusCode();
    
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var updated = await db.Subscriptions.FindAsync(_subId);

        updated!.Name.Should().Be("New Name");
        updated.Note.Should().Be("Updated Note");
        updated.Cost.Should().Be(15);
        updated.Currency.Should().Be(Currency.EUR);
        updated.Plan.Should().Be(SubscriptionPlan.Custom);
    }
    
    [Fact]
    public async Task Handle_SubscriptionDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest();
        var randomSubId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync($"/api/subscriptions/{randomSubId}", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Handle_SubscriptionBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleUserAsync(_otherUserId);
        await factory.CreateExampleSubscriptionAsync(_subId, _otherUserId, DateTime.UtcNow);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
        var request = CreateValidRequest();

        // Act
        var response = await client.PutAsJsonAsync($"/api/subscriptions/{_subId}", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_UpdatePlanAndStartDate_ShouldUpdateBothCorrectly()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow.AddDays(1));

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest(
            startDate: DateTime.UtcNow.AddDays(5),
            plan: SubscriptionPlan.Custom,
            cost: 100);

        // Act
        var response = await client.PutAsJsonAsync($"/api/subscriptions/{_subId}", request);
        
        // Assert
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var updated = await db.Subscriptions.FindAsync(_subId);

        updated!.Plan.Should().Be(SubscriptionPlan.Custom);
        updated.StartDate.Date.Should().Be(request.StartDate.Date);
        updated.Cost.Should().Be(100);
    }

    [Fact]
    public async Task Handle_DuplicateName_ShouldReturnConflict()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var sub2Id = Guid.NewGuid();
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow, "Netflix");
        await factory.CreateExampleSubscriptionAsync(sub2Id, _userId, DateTime.UtcNow, "Hulu");

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = CreateValidRequest(name: "Netflix");

        // Act
        var response = await client.PutAsJsonAsync($"/api/subscriptions/{sub2Id}", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    private static UpdateSubscriptionRequest CreateValidRequest(
        string name = "New Name", 
        SubscriptionPlan plan = SubscriptionPlan.Custom,
        decimal? cost = null,
        DateTime? startDate = null,
        int? customPeriodInDays = 15,
        int? trialPeriodInDays = null,
        string? note = "Updated Note")
    {
        return new UpdateSubscriptionRequest(
            Name: name,
            Plan: plan.ToString(),
            CustomPeriodInDays: customPeriodInDays,
            TrialPeriodInDays: trialPeriodInDays,
            StartDate: startDate ?? DateTime.UtcNow,
            Cost: cost ?? 15,
            Currency: "EUR",
            Note: note,
            CancelLink: null,
            PicLink: null
        );
    }
}