using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenuMate.Api.Persistence;
using RenuMate.Api.Reminders.Create;

namespace RenuMate.Api.Tests.Integration.Reminders;

public class CreateReminderTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _subId = Guid.NewGuid();
    
    [Fact]
    public async Task Handle_ValidRequest_CreatesOccurrenceSuccessfully()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
       
        var request = new CreateReminderRequest(
            SubscriptionId: _subId,
            NotifyTime: new TimeSpan(10, 0, 0),
            Timezone: "GMT Standard Time",
            DaysBeforeRenewal: 2
        );
        
        // Act
        var response = await client.PostAsJsonAsync("/api/reminders", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateReminderResponse>();
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var occurrences = await db.ReminderOccurrences
            .Where(o => o.ReminderRuleId == result.Id)
            .ToListAsync();
        
        occurrences.Should().NotBeEmpty();
        occurrences.Should().ContainSingle(o => !o.IsSent);
        
        occurrences.First().ScheduledAt.TimeOfDay.Should().Be(request.NotifyTime);
    }
    
    [Fact]
    public async Task Handle_SubscriptionNotFound_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        var request = new CreateReminderRequest(
            SubscriptionId: Guid.NewGuid(), 
            NotifyTime: new TimeSpan(10, 0, 0),
            Timezone: "GMT Standard Time",
            DaysBeforeRenewal: 2
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/reminders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Handle_DuplicateRule_ReturnsConflict()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow.AddMonths(1));
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());

        var request = new CreateReminderRequest(_subId, new TimeSpan(10,0,0), "GMT Standard Time", 3);

        // Act
        await client.PostAsJsonAsync("/api/reminders", request);
        var secondResponse = await client.PostAsJsonAsync("/api/reminders", request); 

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict); 
    }
    
    [Fact]
    public async Task Handle_ReminderTimeAlreadyPassedForCurrentCycle_SchedulesNextCycle()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        var renewalDate = DateTime.UtcNow.AddDays(2); 
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, renewalDate);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        var passedTime = DateTime.UtcNow.AddHours(-1).TimeOfDay;
    
        var request = new CreateReminderRequest(
            SubscriptionId: _subId,
            NotifyTime: passedTime,
            Timezone: "UTC",
            DaysBeforeRenewal: 2 
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/reminders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        var occurrence = await db.ReminderOccurrences.FirstAsync(o => o.ReminderRule.SubscriptionId == _subId);

        occurrence.ScheduledAt.Should().BeAfter(DateTime.UtcNow);
    }
    
    [Fact]
    public async Task Handle_InvalidTimeZone_ReturnsBadRequest()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        var client = factory.GetAuthenticatedClient(userId: _userId.ToString());
    
        var request = new CreateReminderRequest(
            SubscriptionId: _subId, 
            NotifyTime: new TimeSpan(10, 0, 0),
            Timezone: "trdfjknvgjkrengfkjtr",
            DaysBeforeRenewal: 2
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/reminders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task Handle_UserDoesntOwnSubscription_ReturnsNotFound()
    {
        // Arrange
        await factory.ResetDatabaseAsync();
        await factory.CreateExampleUserAsync(_userId);
        await factory.CreateExampleSubscriptionAsync(_subId, _userId, DateTime.UtcNow);
        var client = factory.GetAuthenticatedClient(userId: Guid.NewGuid().ToString());
    
        var request = new CreateReminderRequest(
            SubscriptionId: _subId, 
            NotifyTime: new TimeSpan(10, 0, 0),
            Timezone: "trdfjknvgjkrengfkjtr",
            DaysBeforeRenewal: 2
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/reminders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}