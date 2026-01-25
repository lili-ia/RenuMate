using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Email;
using RenuMate.Api.Users.RequestReactivate;

namespace RenuMate.Api.Tests.Integration.Users;

public class RequestUserReactivateTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private const string Email = "test@example.com";
    
    [Fact]
    public async Task Handle_UserDoesNotExist_ReturnsOkNoEmailSent()
    {
        // Arrange
        
        await factory.ResetDatabaseAsync();
        var client = factory.CreateDefaultClient();

        var request = new ReactivateUserRequest(Email);

        // Act
        var response = await client.PostAsJsonAsync("/api/users/reactivate-request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        factory.EmailSenderMock.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task Handle_UserIsActive_ReturnsOk_NoEmailSent()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, verified: true, isActive: true);
        var client = factory.CreateDefaultClient();
        
        var request = new ReactivateUserRequest(Email);
    
        // Act
        var response = await client.PostAsJsonAsync("/api/users/reactivate-request", request);
    
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    
        factory.EmailSenderMock.Verify(
            x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task Handle_UserIsDeactivated_SendsEmailReturnsOk()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, email: Email, verified: true, isActive: false);
    
        factory.TokenServiceMock
            .Setup(x => x.CreateToken(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                "Reactivate", 
                "true", 
                It.IsAny<DateTime>()))
            .Returns("fake-token");
    
        factory.EmailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSenderResponse(IsSuccess: true, null));
    
        var client = factory.CreateDefaultClient();
    
        var request = new ReactivateUserRequest(Email);
    
        // Act
        var response = await client.PostAsJsonAsync("/api/users/reactivate-request", request);
    
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    
        factory.EmailSenderMock.Verify(
            x => x.SendEmailAsync(
                Email,
                "Reactivate your account",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact] 
    public async Task Handle_EmailFails_ReturnsOk()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, email: Email, verified: true, isActive: false);
    
        factory.TokenServiceMock
            .Setup(x => x.CreateToken(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                "Reactivate", 
                "true", 
                It.IsAny<DateTime>()))
            .Returns("fake-token");

        factory.EmailSenderMock
            .Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSenderResponse(IsSuccess: false, It.IsAny<string>()));
    
        var client = factory.CreateDefaultClient();
    
        var request = new ReactivateUserRequest(Email);
    
        // Act
        var response = await client.PostAsJsonAsync("/api/users/reactivate-request", request);
    
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact] 
    public async Task Handle_EmailFails_CreatesPendingEmail()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, email: Email, verified: true, isActive: false);
    
        factory.TokenServiceMock
            .Setup(x => x.CreateToken(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                "Reactivate", 
                "true", 
                It.IsAny<DateTime>()))
            .Returns("fake-token");

        factory.EmailSenderMock
            .Setup(x => x.SendEmailAsync(
                Email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSenderResponse(IsSuccess: false, It.IsAny<string>()));
    
        var client = factory.CreateDefaultClient();
        var request = new ReactivateUserRequest(Email);
    
        // Act
        await client.PostAsJsonAsync("/api/users/reactivate-request", request);
    
        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var emails = await db.PendingEmails
            .Take(1)
            .ToListAsync();

        emails.Count.Should().Be(1);
        emails.Last().To.Should().Be(Email);
        emails.Last().IsSent.Should().BeFalse();
        emails.Last().RetryCount.Should().Be(0);
    }
    
    [Fact]
    public async Task Handle_DeactivatedUser_GeneratesToken()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId, email: Email, verified: true, isActive: false);
    
        var client = factory.CreateDefaultClient();
    
        var request = new ReactivateUserRequest(Email);
        
        factory.TokenServiceMock.Setup(x => x.CreateToken(
            _userId.ToString(), 
            Email,
            "Reactivate",
            "true",
            It.IsAny<DateTime>()
            )).Returns("fghjkjgfdghlihghjklhghjhdfhgvhjdsjhfvhjfhjd");
        
        // Act
        await client.PostAsJsonAsync("/api/users/reactivate-request", request);
    
        // Assert
        factory.TokenServiceMock.Verify(
            x => x.CreateToken(
                _userId.ToString(),
                Email,
                "Reactivate",
                "true",
                It.IsAny<DateTime>()),
            Times.Once);
        
        factory.EmailSenderMock.Verify(
            x => x.SendEmailAsync(
                Email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}