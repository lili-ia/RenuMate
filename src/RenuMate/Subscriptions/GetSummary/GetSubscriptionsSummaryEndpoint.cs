using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using RenuMate.Common;
using RenuMate.DTOs;
using RenuMate.Enums;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.GetSummary;

public class GetSubscriptionsSummaryEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/summary", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Get summary for user's subscriptions.")
        .WithDescription("Calculates a financial overview of all active subscriptions. " +
                         "Costs are normalized to the requested period (daily, weekly, monthly, yearly) " +
                         "and converted to the target currency. 'ProjectedCost' includes trials " +
                         "assuming they convert to a monthly plan.")
        .WithTags("Subscriptions")
        .Produces<SubscriptionSummaryDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);
    
    private static async Task<IResult> Handle(
        [FromQuery] string currency, 
        [FromQuery] string period,
        IUserContext userContext, 
        RenuMateDbContext db, 
        ICurrencyService currencyService,
        IMemoryCache cache,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var cacheKey = $"summary_user_{userId}_{currency}_{period}";
        
        var signalKey = $"signal_{userId}";
        var cts = cache.GetOrCreate(signalKey, _ => new CancellationTokenSource());

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(24))
            .AddExpirationToken(new CancellationChangeToken(cts!.Token));
        
        
        if (cache.TryGetValue(cacheKey, out SubscriptionSummaryDto? summary))
        {
            return Results.Ok(summary);
        }
        
        if (!Enum.TryParse<Currency>(currency, true, out var targetCurrency))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid currency code"
            );
        }
        
        var requestedPeriodDays = period.ToLowerInvariant() switch {
            "daily" => 1,
            "weekly" => 7,
            "yearly" => 365,
            _ => 30 
        };

        var activeSubscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsMuted)
            .Select(s => new { s.Currency, s.Cost, s.Plan, s.CustomPeriodInDays })
            .ToListAsync(cancellationToken);

        var activeReminders = await db.ReminderRules
            .CountAsync(r => r.Subscription.UserId == userId && !r.Subscription.IsMuted, cancellationToken);
        
        if (activeSubscriptions.Count == 0)
        {
            summary = new SubscriptionSummaryDto(0, 0, 0, 0);
            cache.Set(cacheKey, summary, cacheOptions);
            
            return Results.Ok(summary);
        }

        var rates = await currencyService.GetRateForDesiredCurrency(targetCurrency, cancellationToken);
        
        if (rates is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Currency service error"
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