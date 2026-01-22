using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using RenuMate.Api.DTOs;
using RenuMate.Api.Enums;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.GetSummary;

public class GetSubscriptionsSummaryCommandHandler(RenuMateDbContext db, IMemoryCache cache, ICurrencyService currencyService) 
    : IRequestHandler<GetSubscriptionsSummaryCommand, IResult>
{
    public async Task<IResult> Handle(GetSubscriptionsSummaryCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = $"summary_user_{request.UserId}_{request.Currency}_{request.Period}";
        var signalKey = $"signal_{request.UserId}";
        var cts = cache.GetOrCreate(signalKey, _ => new CancellationTokenSource());

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(24))
            .AddExpirationToken(new CancellationChangeToken(cts!.Token));
        
        if (cache.TryGetValue(cacheKey, out SubscriptionSummaryDto? summary))
        {
            return Results.Ok(summary);
        }
        
        var requestedPeriodDays = request.Period.ToLowerInvariant() switch {
            "daily" => 1,
            "weekly" => 7,
            "yearly" => 365,
            _ => 30 
        };

        var activeSubscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId && !s.IsMuted)
            .Select(s => new { s.Currency, s.Cost, s.Plan, s.CustomPeriodInDays })
            .ToListAsync(cancellationToken);

        var activeReminders = await db.ReminderRules
            .CountAsync(r => r.Subscription.UserId == request.UserId 
                             && !r.Subscription.IsMuted, cancellationToken);
        
        if (activeSubscriptions.Count == 0)
        {
            summary = new SubscriptionSummaryDto(0, 0, 0, 0);
            cache.Set(cacheKey, summary, cacheOptions);
            
            return Results.Ok(summary);
        }

        var rates = await currencyService.GetRateForDesiredCurrency(request.Currency, cancellationToken);
        
        if (rates is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Currency service unavailable"
            );
        }
        
        decimal currentTotal = 0;
        decimal projectedTotal = 0;

        foreach (var sub in activeSubscriptions)
        {
            if (!rates.TryGetValue(sub.Currency, out var rate) || rate == 0)
            {
                continue;
            }
            
            var costInTarget = sub.Cost / rate;

            var dailyCost = sub.Plan switch
            {
                SubscriptionPlan.Trial => 0,
                SubscriptionPlan.Monthly => costInTarget / 30,
                SubscriptionPlan.Quarterly => costInTarget / 91, 
                SubscriptionPlan.Annual => costInTarget / 365,
                SubscriptionPlan.Custom => costInTarget / (sub.CustomPeriodInDays ?? 30),
                _ => costInTarget / 30
            };

            var projectedDailyCost = sub.Plan == SubscriptionPlan.Trial 
                ? costInTarget / 30 
                : dailyCost;

            currentTotal += dailyCost * requestedPeriodDays;
            projectedTotal += projectedDailyCost * requestedPeriodDays;
        }
        
        summary = new SubscriptionSummaryDto
        (
            TotalCost: Math.Round(currentTotal, 2),
            ProjectedCost: Math.Round(projectedTotal, 2),
            ActiveSubscriptionsCount: activeSubscriptions.Count,
            ActiveRemindersCount: activeReminders
        );
        
        cache.Set(cacheKey, summary, cacheOptions);
        
        return Results.Ok(summary);
    }
}