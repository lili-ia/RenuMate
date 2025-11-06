using System.Linq.Expressions;
using RenuMate.DTOs;
using RenuMate.Entities;

namespace RenuMate.Subscriptions;

public static class SubscriptionMapper
{
    public static Expression<Func<Subscription, SubscriptionDto>> ProjectToDto => subscription =>
        new SubscriptionDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Plan = subscription.Plan,
            CustomPeriodInDays = subscription.CustomPeriodInDays,
            StartDate = subscription.StartDate,
            RenewalDate = subscription.RenewalDate,
            Cost = subscription.Cost,
            Currency = subscription.Currency,
            IsMuted = subscription.IsMuted,
            Note = subscription.Note,
            CancelLink = subscription.CancelLink,
            PicLink = subscription.PicLink
        };
    public static Expression<Func<Subscription, SubscriptionDto>> ProjectToDetailsDto => subscription =>
        new SubscriptionDetailsDto
        {
            Id = subscription.Id,
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
            IsMuted = subscription.IsMuted,
            Reminders = subscription.Reminders
                .Select(r => new ReminderDto
                {
                    Id = r.Id,
                    DaysBeforeRenewal = r.DaysBeforeRenewal,
                    NotifyTime = r.NotifyTimeUtc,
                    NextReminder = subscription.RenewalDate.Date
                        .AddDays(-r.DaysBeforeRenewal)
                        .Add(r.NotifyTimeUtc),
                }).ToList()
        };
}