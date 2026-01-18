namespace RenuMate.Api.DTOs;

public sealed record ReminderDto(
    Guid Id,
    int DaysBeforeRenewal,
    TimeSpan NotifyTime,
    DateTime NextReminder
);