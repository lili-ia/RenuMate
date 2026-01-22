using FluentAssertions;
using RenuMate.Api.Entities;
using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Tests.Unit.Entities;

public class ReminderRuleTests
{
    [Fact]
    public void CreateOccurrence_ValidFutureDate_ReturnsCorrectOccurrence()
    {
        // Arrange
        var renewalDate = new DateTime(2026, 1, 10);
        var rule = ReminderRule.Create(Guid.NewGuid(), TimeSpan.FromHours(10), 1);
        var now = new DateTime(2026, 1, 5);

        // Act
        var occurrence = rule.CreateOccurrence(renewalDate, now);

        // Assert
        occurrence.Should().NotBeNull();
        occurrence!.ScheduledAt.Should().Be(new DateTime(2026, 1, 9, 10, 0, 0));
    }

    [Fact]
    public void CreateOccurrence_CalculatedTimeIsInPast_ReturnsNull()
    {
        // Arrange
        var renewalDate = new DateTime(2026, 1, 10);
        var rule = ReminderRule.Create(Guid.NewGuid(), TimeSpan.FromHours(10), 1);
        var now = new DateTime(2026, 1, 9, 11, 0, 0); 

        // Act
        var occurrence = rule.CreateOccurrence(renewalDate, now);

        // Assert
        occurrence.Should().BeNull();
    }

    [Fact]
    public void AddOccurrence_SameScheduledTime_ThrowsDomainConflictException()
    {
        // Arrange
        var rule = ReminderRule.Create(Guid.NewGuid(), TimeSpan.FromHours(10), 1);
        var time = DateTime.UtcNow.AddDays(10);
        var occ1 = ReminderOccurrence.Create(rule.Id, time, DateTime.UtcNow);
        var occ2 = ReminderOccurrence.Create(rule.Id, time, DateTime.UtcNow);
        rule.AddOccurrence(occ1);

        // Act
        var action = () => rule.AddOccurrence(occ2);

        // Assert
        action.Should().Throw<DomainConflictException>()
            .WithMessage($"A reminder occurrence for this rule is already scheduled at {occ2.ScheduledAt}.");
    }
}