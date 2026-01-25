using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Tests.Integration.Users;

public class DeactivateUserEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    private readonly Guid _ruleId = Guid.NewGuid();

    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldSuccessfullyDeactivateAccount()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        await factory.CreateExampleUserAsync(_userId, verified: true);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MessageResponse>();
        payload.Should().NotBeNull();
        payload.Message.Should().Contain("successfully deactivated");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        
        var user = await db.Users.SingleAsync(u => u.Id == _userId);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserDoesntExist_ShouldReturnNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.DeleteAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
    
        var client = factory.CreateDefaultClient();
        client.DefaultRequestHeaders.Clear();
    
        // Act
        var response = await client.DeleteAsync("/api/users");
    
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Handle_ShouldMuteAllSubscriptions()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
    
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
    
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        // Act
        await client.DeleteAsync("/api/users/me");
    
        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        
        var subs = await db.Subscriptions.Where(s => s.UserId == _userId).ToListAsync();
    
        subs.Should().OnlyContain(s => s.IsMuted);
    }
    
    [Fact]
    public async Task DeactivateUser_SetsUserInactive()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
    
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        // Act
        await client.DeleteAsync("/api/users/me");
    
        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        
        var user = await db.Users.FindAsync(_userId);
        user!.IsActive.Should().BeFalse();
    }
    
    [Fact]
    public async Task DeactivateUser_ClearsReminderRules()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
    
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRuleAsync(_ruleId, null, _subId, createOccurrence: false);
    
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        // Act
        await client.DeleteAsync("/api/users/me");
    
        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        
        var rules = await db.ReminderRules.Where(r => r.SubscriptionId == _subId).ToListAsync();
    
        rules.Should().BeEmpty();
    }
    
    [Fact]
    public async Task DeactivateUser_RemovesUnsentReminderOccurrences()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        await factory.CreateExampleReminderRuleAsync(_ruleId, Guid.NewGuid(), _subId, createOccurrence: true);
        await factory.CreateExampleReminderOccurrenceAsync(_ruleId, Guid.NewGuid(), isSent: true);
    
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        // Act
        await client.DeleteAsync("/api/users/me");
    
        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var remaining = await db.ReminderOccurrences.ToListAsync();
    
        remaining.Should().ContainSingle(o => o.IsSent);
    }
}
