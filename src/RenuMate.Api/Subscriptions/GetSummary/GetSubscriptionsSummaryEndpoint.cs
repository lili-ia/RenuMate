using System.Net.Mime;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Api.Common;
using RenuMate.Api.DTOs;
using RenuMate.Api.Enums;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.GetSummary;

public class GetSubscriptionsSummaryEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/summary", Handle)
        .RequireAuthorization("ActiveUserOnly")
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
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        if (!Enum.TryParse<Currency>(currency, true, out var targetCurrency))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid currency code"
            );
        }

        var command = new GetSubscriptionsSummaryCommand(
            UserId: userId,
            Currency: targetCurrency,
            Period: period);

        var result = await mediator.Send(command, cancellationToken);

        return result;
    }
}