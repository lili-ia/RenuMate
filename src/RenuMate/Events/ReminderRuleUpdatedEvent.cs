using MediatR;

namespace RenuMate.Events;

public sealed record ReminderRuleUpdatedEvent(
    Guid ReminderRuleId, 
    int DaysBeforeRenewal, 
    TimeSpan NotifyTime) : INotification;