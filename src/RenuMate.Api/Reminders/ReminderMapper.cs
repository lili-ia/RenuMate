using System.Linq.Expressions;
using RenuMate.Api.DTOs;
using RenuMate.Api.Entities;

namespace RenuMate.Api.Reminders;

public static class ReminderMapper
{
    public static Expression<Func<ReminderRule, ReminderDto>> ProjectToDto => reminder =>
        new ReminderDto
        (
            reminder.Id,
            reminder.DaysBeforeRenewal,
            reminder.NotifyTimeUtc,
            reminder.Subscription.RenewalDate.Date
                .AddDays(-reminder.DaysBeforeRenewal)
                .Add(reminder.NotifyTimeUtc)
        );
}