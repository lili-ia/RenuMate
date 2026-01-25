using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Tests.Integration.Users;

public class ReactivateUserEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Handle_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var client = factory.GetAuthenticatedClient();

        factory.TokenServiceMock
            .Setup(x => x.ValidateToken(It.IsAny<string>(), "Reactivate"))
            .Returns((ClaimsPrincipal?)null);
        
        // Act
        var response = await client.PatchAsync($"/api/users/me?token=fake-token", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsBadRequest()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        var client = factory.GetAuthenticatedClient();

        // TokenService returns principal with a user ID that doesn't exist in DB
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString())
        }));

        factory.TokenServiceMock
            .Setup(x => x.ValidateToken(It.IsAny<string>(), "Reactivate"))
            .Returns(claims);
        
        // Act
        var response = await client.PatchAsync("/api/users/me?token=some-token", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_ValidToken_ReactivatesUserAndReturnsToken()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, isActive: false);

        var client = factory.GetAuthenticatedClient();

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString())
        }));

        factory.TokenServiceMock
            .Setup(x => x.ValidateToken(It.IsAny<string>(), "Reactivate"))
            .Returns(claims);

        factory.Auth0ServiceMock
            .Setup(x => x.SetUserBlockStatusAsync(
                It.IsAny<string>(), 
                false, 
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        factory.TokenServiceMock
            .Setup(x => x.CreateToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Reactivate",
                "true",
                It.IsAny<DateTime>()))
            .Returns("fake-jwt-token");
        
        // Act
        var response = await client.PatchAsync("/api/users/me?token=valid-token", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        result!.Token.Should().Be("fake-jwt-token");
        
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        
        var updatedUser = await db.Users.FindAsync(_userId);
        updatedUser!.IsActive.Should().BeTrue();

        factory.Auth0ServiceMock.Verify(
            x => x.SetUserBlockStatusAsync(
                It.IsAny<string>(),
                false, 
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
