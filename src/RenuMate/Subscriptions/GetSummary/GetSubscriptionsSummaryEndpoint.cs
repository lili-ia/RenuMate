using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        .WithDescription("") // todo???
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
         CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        if (!Enum.TryParse<Currency>(currency, true, out var targetCurrency))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid currency code",
                detail: "Invalid currency code provided."
            );
        }
        
        var requestedPeriod = period.ToLowerInvariant() switch {
            "daily" => 1,
            "weekly" => 7,
            "yearly" => 365,
            _ => 30 
        };
        
        var stats = await db.Subscriptions
            .Where(s => s.UserId == userId)
            .GroupBy(s => 1) 
            .Select(g => new {
                ActiveCount = g.Count(s => !s.IsMuted),
                TotalReminders = db.ReminderRules.Count(r => r.Subscription.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsMuted)
            .Select(s => new { s.Currency, s.Cost, s.Plan, s.CustomPeriodInDays })
            .ToListAsync(cancellationToken);
        
        if (subscriptions.Count == 0)
        {
            return Results.Ok(0.0);
        }

        var rates = await currencyService.GetRateForDesiredCurrency(targetCurrency);

        if (rates is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: $"An internal error occured while trying to retrieve currency rates for {targetCurrency}."
            );
        }
        
        decimal totalNormalized = 0;
        
        foreach (var sub in subscriptions)
        {
            if (!rates.TryGetValue(sub.Currency, out var rate))
            {
                continue;
            }
        
            var costInTarget = sub.Cost / rate;

            var dailyCost = sub.Plan switch
            {
                SubscriptionPlan.Monthly => costInTarget / 30,
                SubscriptionPlan.Annual => costInTarget / 365,
                SubscriptionPlan.Quarterly => costInTarget / 91, 
                SubscriptionPlan.Custom => costInTarget / (sub.CustomPeriodInDays ?? 30),
                _ => costInTarget / 30
            };

            totalNormalized += dailyCost * requestedPeriod;
        }

        return Results.Ok(new SubscriptionSummaryDto
        (
            TotalCost: Math.Round(totalNormalized, 2),
            ActiveSubscriptionsCount: stats?.ActiveCount ?? 0,
            TotalRemindersCount: stats?.TotalReminders ?? 0
        ));
    }
}