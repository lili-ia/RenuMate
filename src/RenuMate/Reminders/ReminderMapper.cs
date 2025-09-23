using System.Linq.Expressions;
using RenuMate.DTOs;
using RenuMate.Entities;

namespace RenuMate.Reminders;

public static class ReminderMapper
{
    public static Expression<Func<ReminderRule, ReminderDto>> ProjectToDto => reminder =>
        new ReminderDto
        {
            Id = reminder.Id,
            DaysBeforeRenewal = reminder.DaysBeforeRenewal,
            NotifyTime = reminder.NotifyTime,
            NextReminder = reminder.Subscription.RenewalDate.Date
                .AddDays(-reminder.DaysBeforeRenewal)
                .Add(reminder.NotifyTime)
        };
}