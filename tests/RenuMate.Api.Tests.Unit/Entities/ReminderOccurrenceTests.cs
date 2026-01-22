using FluentAssertions;
using RenuMate.Api.Entities;
using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Tests.Unit.Entities;

public class ReminderOccurrenceTests
{
    [Fact]
    public void Create_ScheduledTimeInPast_ThrowsDomainConflictException()
    {
        // Arrange
        var now = new DateTime(2026, 1, 19, 12, 0, 0);
        var pastTime = now.AddMinutes(-1);
        var ruleId = Guid.NewGuid();

        // Act
        var action = () => ReminderOccurrence.Create(ruleId, pastTime, now);

        // Assert
        action.Should().Throw<DomainConflictException>()
            .WithMessage("Cannot schedule a reminder in the past.");
    }

    [Fact]
    public void MarkAsSent_FirstTime_SetsSentProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var scheduledAt = now.AddHours(1);
        var occ = ReminderOccurrence.Create(Guid.NewGuid(), scheduledAt, now);

        // Act
        occ.MarkAsSent();

        // Assert
        occ.IsSent.Should().BeTrue();
        occ.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsSent_AlreadySent_ThrowsDomainConflictException()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var occ = ReminderOccurrence.Create(Guid.NewGuid(), now.AddHours(1), now);
        occ.MarkAsSent();

        // Act
        var action = () => occ.MarkAsSent();

        // Assert
        action.Should().Throw<DomainConflictException>()
            .WithMessage("Reminder has already been sent.");
    }
}