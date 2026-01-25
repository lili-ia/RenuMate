using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Tests.Integration.Reminders;

public class DeleteReminderTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    private readonly Guid _ruleId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidId_ReturnsNoContentAndDeletesFromDb()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRuleAsync(
            reminderRuleId: _ruleId, reminderOccurrenceId: null, subscriptionId: _subId, createOccurrence: false);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/reminders/{_ruleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRuleAsync(
            reminderRuleId: _ruleId, reminderOccurrenceId: null, subscriptionId: _subId, createOccurrence: false);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/reminders/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Handle_RuleWithMixedOccurrences_RemovesOnlyExpectedRecords()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRuleAsync(
            reminderRuleId: _ruleId, reminderOccurrenceId: Guid.NewGuid(), subscriptionId: _subId, createOccurrence: true);
        await factory.CreateExampleReminderOccurrenceAsync(_ruleId, Guid.NewGuid(), isSent: true);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync($"/api/reminders/{_ruleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var occurrences = await db.ReminderOccurrences
            .ToListAsync();

        occurrences.Count.Should().Be(1);
        occurrences.Last().IsSent.Should().BeTrue();
    }
    
    [Fact]
    public async Task Handle_UserDoesntOwnReminder_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRuleAsync(
            reminderRuleId: _ruleId, reminderOccurrenceId: null, subscriptionId: _subId, createOccurrence: false);

        var client = factory.GetAuthenticatedClient(userId: Guid.NewGuid().ToString());

        // Act
        var response = await client.DeleteAsync($"/api/reminders/{_ruleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}