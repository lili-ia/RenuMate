using System.Net;
using System.Net.Http.Headers;
using Auth0.Core.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Tests.Integration.Users;

public class SyncUserTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private const string Email = "some.email@example.com";
    private const string Sub = "auth0|some-user";
    private const string Name = "Name";
    
    [Fact]
    public async Task Handle_NewUser_ShouldCreateNewAccount()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.Auth0ServiceMock.Invocations.Clear();
        
        var client = factory.GetAuthenticatedClient(Sub, Email);
        
        // Act
        var response = await client.PostAsync("/api/users/sync-user", null);
        
        // Assert
        factory.Auth0ServiceMock.Verify(
            x => x.UpdateUserInternalIdAsync(
                It.IsAny<string>(), 
                It.IsAny<Guid>(), 
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == Email);
        user.Auth0Id.Should().Be(Sub);
        user.Email.Should().Be(Email);
        user.IsMetadataSynced.Should().Be(true);
    }
    
    [Fact]
    public async Task Handle_UserExists_ShouldUpdateNameAndEmail()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.Auth0ServiceMock.Invocations.Clear();

        await factory.CreateExampleUserAsync(_userId, Email, Sub, Name);
        
        var client = factory.GetAuthenticatedClient(Sub, "new@example.com", "New Name");
    
        // Act
        var response = await client.PostAsync("/api/users/sync-user", null);
    
        // Assert
        response.EnsureSuccessStatusCode();
        
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var user = await db.Users.SingleAsync(u => u.Auth0Id == Sub);
        user.Email.Should().Be("new@example.com");
        user.Name.Should().Be("New Name");
    }
    
    [Fact]
    public async Task Handle_NewUser_Auth0Fails_ShouldNotCreateUserAndReturnBadGateway()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.Auth0ServiceMock.Invocations.Clear();

        factory.Auth0ServiceMock
            .Setup(x => x.UpdateUserInternalIdAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ErrorApiException("Auth0 API Down"));

        var client = factory.GetAuthenticatedClient(Sub, Email);

        // Act
        var response = await client.PostAsync("/api/users/sync-user", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var usersCount = await db.Users.CountAsync();
        usersCount.Should().Be(0);

        factory.Auth0ServiceMock.Verify(
            x => x.UpdateUserInternalIdAsync(
                Sub,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_SameRequestTwice_ShouldNotCreateDuplicateUser()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        factory.Auth0ServiceMock.Invocations.Clear();

        var client = factory.GetAuthenticatedClient(Sub, Email);

        // Act
        var firstResponse = await client.PostAsync("/api/users/sync-user", null);
        var secondResponse = await client.PostAsync("/api/users/sync-user", null);

        // Assert
        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var users = await db.Users.ToListAsync();

        users.Should().HaveCount(1);
        users.Single().Auth0Id.Should().Be(Sub);

        factory.Auth0ServiceMock.Verify(
            x => x.UpdateUserInternalIdAsync(
                Sub,
                users.Single().Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_MissingRequiredClaims_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        
        // Act
        var response = await client.PostAsync("/api/users/sync-user", null);
    
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}