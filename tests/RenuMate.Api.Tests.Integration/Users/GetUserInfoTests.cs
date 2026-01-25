using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Users.GetInfo;

namespace RenuMate.Api.Tests.Integration.Users;

public class GetUserInfoTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    
    [Fact]
    public async Task Handle_ActiveUser_ReturnsUserInfo()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, email: "test@example.com", name: "Test User");

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var response = await client.GetFromJsonAsync<UserInfoResponse>("/api/users/me");

        // Assert
        response.Should().NotBeNull();
        response!.Id.Should().Be(_userId);
        response.Email.Should().Be("test@example.com");
        response.Name.Should().Be("Test User");
        response.SubscriptionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserInfo_NonexistentUser_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        var client = factory.GetAuthenticatedClient(userId: Guid.NewGuid().ToString());

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserInfo_UsesCache_OnSecondCall()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);

        var cache = factory.Services.GetRequiredService<IMemoryCache>();
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        // Act
        var first = await client.GetFromJsonAsync<UserInfoResponse>("/api/users/me");
        var second = await client.GetFromJsonAsync<UserInfoResponse>("/api/users/me");

        // Assert
        second.Should().BeEquivalentTo(first);

        cache.TryGetValue($"info_user_{_userId}", out UserInfoResponse? cachedInfo).Should().BeTrue();
        cachedInfo!.Id.Should().Be(_userId);
    }

    [Fact]
    public async Task GetUserInfo_InactiveUser_ReturnsForbidden()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, isActive: false);

        var client = factory.GetAuthenticatedClient(userId: _userId.ToString(), isActive: false);

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem?.Title.Should().Be("Account Deactivated");
    }
}