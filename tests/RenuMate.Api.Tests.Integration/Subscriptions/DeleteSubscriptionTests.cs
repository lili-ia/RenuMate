using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Tests.Integration.Subscriptions;

public class DeleteSubscriptionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    private readonly Guid _otherSubId = Guid.NewGuid();
    private readonly Guid _ruleId = Guid.NewGuid();
    private readonly Guid _occurrenceId = Guid.NewGuid();

    [Fact]
    public async Task Handle_SuccessfulDeletion_ReturnsNoContent()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/subscriptions/{_subId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var exists = await db.Subscriptions.AnyAsync(s => s.Id == _subId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_OtherUserSubscription_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleUserAsync(_otherUserId);
        await factory.CreateExampleSubscriptionAsync(_otherSubId, _otherUserId, DateTime.UtcNow);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/subscriptions/{_otherSubId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Handle_SuccessfulDeletion_DeletesRulesAndSetsReminderItToNullOnOccurrences()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRule(_ruleId, _occurrenceId, _subId, createOccurrence: true);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/subscriptions/{_subId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var occurrences = await db.ReminderOccurrences.ToListAsync();

        occurrences.Should().NotBeEmpty();
        occurrences.Should().OnlyContain(o => o.ReminderRuleId == null);

        var rulesCount = await db.ReminderRules.CountAsync();

        rulesCount.Should().Be(0);
    }
}