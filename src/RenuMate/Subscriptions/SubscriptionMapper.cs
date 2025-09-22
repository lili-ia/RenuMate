using System.Linq.Expressions;
using RenuMate.DTOs;
using RenuMate.Entities;

namespace RenuMate.Subscriptions;

public static class SubscriptionMapper
{
    public static Expression<Func<Subscription, SubscriptionDto>> ProjectToDto => subscription =>
        new SubscriptionDto
        {
            Name = subscription.Name,
            Plan = subscription.Plan,
            CustomPeriodInDays = subscription.CustomPeriodInDays,
            StartDate = subscription.StartDate,
            RenewalDate = subscription.RenewalDate,
            Cost = subscription.Cost,
            Currency = subscription.Currency,
            Note = subscription.Note,
            CancelLink = subscription.CancelLink,
            PicLink = subscription.PicLink
        };
    
    public static Expression<Func<Subscription, SubscriptionDto>> ProjectToDetailsDto => subscription =>
        new SubscriptionDetailsDto
        {
            Name = subscription.Name,
            Plan = subscription.Plan,
            CustomPeriodInDays = subscription.CustomPeriodInDays,
            StartDate = subscription.StartDate,
            RenewalDate = subscription.RenewalDate,
            Cost = subscription.Cost,
            Currency = subscription.Currency,
            Note = subscription.Note,
            CancelLink = subscription.CancelLink,
            PicLink = subscription.PicLink,
            Reminders = subscription.Reminders
                .Select(r => new ReminderDto
                {
                    Id = r.Id,
                    DaysBeforeRenewal = r.DaysBeforeRenewal,
                    NotifyTime = r.NotifyTime,
                    NextReminder = subscription.RenewalDate.Date
                        .AddDays(-r.DaysBeforeRenewal)
                        .Add(r.NotifyTime),
                    IsMuted = r.IsMuted
                }).ToList()
        };
}