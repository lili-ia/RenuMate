using RenuMate.Api.Enums;

namespace RenuMate.Api.DTOs;

public record SubscriptionDto(
    Guid Id,
    string Name,
    SubscriptionPlan Plan,
    int? CustomPeriodInDays,
    DateOnly StartDate,
    DateOnly RenewalDate,
    decimal Cost,
    Currency Currency,
    bool IsMuted,
    string? Note,
    string? CancelLink,
    string? PicLink
)
{
    public int DaysLeft => RenewalDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
}