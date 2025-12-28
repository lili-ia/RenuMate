using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Enums;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Analytics.GetTotalInCurrency;

public class GetActiveTotalInCurrencyEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/total", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Get total for subscriptions in desired currency.")
        .WithDescription("") // todo???
        .WithTags("Subscriptions")
        .Produces<decimal>(StatusCodes.Status200OK)
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
        
        var userId = userContext.UserId;
        
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

        return Results.Ok(Math.Round(totalNormalized, 2));
    }
}