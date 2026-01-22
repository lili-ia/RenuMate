using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Persistence;
using RenuMate.Api.Subscriptions.SetMuteStatus;

namespace RenuMate.Api.Tests.Integration.Subscriptions;

public class SetSubscriptionMuteStatusTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    private readonly Guid _otherSubId = Guid.NewGuid();
    
    [Fact]
    public async Task SetMuteStatus_ValidRequest_UpdatesSubscription()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow, isMuted: false);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = new SetSubscriptionMuteStatusRequest(true);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/subscriptions/{_subId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var updated = await db.Subscriptions.FindAsync(_subId);

        updated!.IsMuted.Should().BeTrue();
    }

    [Fact]
    public async Task SetMuteStatus_SubscriptionDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = new SetSubscriptionMuteStatusRequest(true);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/subscriptions/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMuteStatus_OtherUsersSubscription_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleUserAsync(_otherUserId);

        await factory.CreateExampleSubscriptionAsync(_otherSubId, _otherUserId, DateTime.UtcNow, isMuted: false);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
        var request = new SetSubscriptionMuteStatusRequest(true);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/subscriptions/{_otherSubId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMuteStatus_ToggleBackAndForth_WorksCorrectly()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow, isMuted: false);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        await client.PatchAsJsonAsync($"/api/subscriptions/{_subId}", new SetSubscriptionMuteStatusRequest(true));
        await client.PatchAsJsonAsync($"/api/subscriptions/{_subId}", new SetSubscriptionMuteStatusRequest(false));

        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var updated = await db.Subscriptions.FindAsync(_subId);
        
        updated!.IsMuted.Should().BeFalse();
    }
}