using System.Linq.Expressions;
using RenuMate.DTOs;
using RenuMate.Entities;

namespace RenuMate.Subscriptions;

public static class SubscriptionMapper
{
    public static Expression<Func<Subscription, SubscriptionDto>> ProjectToDto => subscription =>
        new SubscriptionDto
        (
            subscription.Id,
            subscription.Name,
            subscription.Plan,
            subscription.CustomPeriodInDays,
            subscription.StartDate,
            subscription.RenewalDate,
            subscription.Cost,
            subscription.Currency,
            subscription.IsMuted,
            subscription.Note,
            subscription.CancelLink,
            subscription.PicLink
        );
    
    public static Expression<Func<Subscription, SubscriptionDetailsDto>> ProjectToDetailsDto => subscription =>
        new SubscriptionDetailsDto
        (
            subscription.Id,
            subscription.Name,
            subscription.Plan,
            subscription.CustomPeriodInDays,
            subscription.StartDate,
            subscription.RenewalDate,
            subscription.Cost,
            subscription.Currency,
            subscription.IsMuted,
            subscription.Note,
            subscription.CancelLink,
            subscription.PicLink,
            subscription.Reminders
                .Select(r => new ReminderDto
                (
                    r.Id,
                    r.DaysBeforeRenewal,
                    r.NotifyTimeUtc,
                    subscription.RenewalDate.Date
                        .AddDays(-r.DaysBeforeRenewal)
                        .Add(r.NotifyTimeUtc)
                )).ToList()
        );
}