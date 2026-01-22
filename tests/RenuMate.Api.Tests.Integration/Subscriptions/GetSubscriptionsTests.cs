using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using RenuMate.Api.Common;
using RenuMate.Api.DTOs;

namespace RenuMate.Api.Tests.Integration.Subscriptions;

public class GetSubscriptionsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    
    [Fact]
    public async Task GetAllSubscriptions_Authenticated_ReturnsOnlyAuthenticatedUsersSubscriptions()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleUserAsync(_otherUserId);

        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, "name 1");
        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _userId, DateTime.UtcNow, "name 2");
        await factory.CreateExampleSubscriptionAsync(Guid.NewGuid(), _otherUserId, DateTime.UtcNow, "name 1");

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        
        // Act
        var response = await client.GetFromJsonAsync<
            PaginatedResponse<SubscriptionDetailsDto>>("/api/subscriptions", options);

        // Assert
        response!.Items.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task GetAllSubscriptions_UnverifiedEmail_ReturnsForbidden()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        await factory.CreateExampleUserAsync(_userId, verified: false);

        var client = factory.GetAuthenticatedClient(
            userId: _userId.ToString(),
            emailVerified: false);

        // Act
        var response = await client.GetAsync("/api/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task GetAllSubscriptions_Pagination_WorksCorrectly()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        await factory.CreateExampleUserAsync(_userId);

        for (int i = 0; i < 15; i++)
        {
            await factory.CreateExampleSubscriptionAsync(
                Guid.NewGuid(), _userId, DateTime.UtcNow.AddDays(i), name: $"sub{i}");
        }

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        
        // Act
        var result = await client.GetFromJsonAsync<
            PaginatedResponse<SubscriptionDetailsDto>>(
            "/api/subscriptions?page=1&pageSize=10", options);

        // Assert
        result!.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetSubscriptionDetails_ReturnsNotFound_ForNonexistentSubscription()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.GetAsync($"/api/subscriptions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubscriptionDetails_ReturnsNotFound_IfBelongsToAnotherUser()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleUserAsync(_otherUserId);

        await factory.CreateExampleSubscriptionAsync(_subId, _otherUserId, DateTime.UtcNow, "Other User Sub");

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.GetAsync($"/api/subscriptions/{_subId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
