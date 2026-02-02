using RenuMate.Api.Enums;

namespace RenuMate.Api.DTOs;

public sealed record SubscriptionDetailsDto(
    Guid Id,
    string Name,
    SubscriptionPlan Plan,
    int? CustomPeriodInDays,
    int? TrialPeriodInDays,
    DateOnly StartDate,
    DateOnly RenewalDate,
    decimal Cost,
    Currency Currency,
    bool IsMuted,
    string? Note,
    string? CancelLink,
    string? PicLink,
    List<ReminderDto> Reminders,
    List<TagDto> Tags
) : SubscriptionDto(
    Id, Name, Plan, CustomPeriodInDays, TrialPeriodInDays, StartDate, RenewalDate, Cost, Currency, IsMuted, Note, CancelLink, PicLink);