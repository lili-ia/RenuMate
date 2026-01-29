using System.Linq.Expressions;
using RenuMate.Api.DTOs;
using RenuMate.Api.Entities;

namespace RenuMate.Api.Subscriptions;

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
                    subscription.RenewalDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                        .AddDays(-r.DaysBeforeRenewal)
                        .Add(r.NotifyTimeUtc),
                    subscription.RenewalDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                        .AddDays(-r.DaysBeforeRenewal)
                        .Add(r.NotifyTimeUtc) < DateTime.UtcNow
                )).ToList()
        );
}