using RenuMate.Enums;

namespace RenuMate.DTOs;

public sealed record SubscriptionDetailsDto(
    Guid Id,
    string Name,
    SubscriptionPlan Plan,
    int? CustomPeriodInDays,
    DateTime StartDate,
    DateTime RenewalDate,
    decimal Cost,
    Currency Currency,
    bool IsMuted,
    string? Note,
    string? CancelLink,
    string? PicLink,
    List<ReminderDto> Reminders
) : SubscriptionDto(Id, Name, Plan, CustomPeriodInDays, StartDate, RenewalDate, Cost, Currency, IsMuted, Note, CancelLink, PicLink);