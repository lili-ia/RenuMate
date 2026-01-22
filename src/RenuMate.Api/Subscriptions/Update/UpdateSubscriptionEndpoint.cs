using System.Net.Mime;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Api.Common;
using RenuMate.Api.Enums;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Subscriptions.Update;

public abstract class UpdateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPut("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .AddEndpointFilter<InvalidateSummaryCacheEndpointFilter>()
        .WithSummary("Update subscription.")
        .WithDescription("Updates the details of a subscription owned by the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<UpdateSubscriptionResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromBody] UpdateSubscriptionRequest request,
        IUserContext userContext,
        IValidator<UpdateSubscriptionRequest> validator,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        Enum.TryParse<SubscriptionPlan>(request.Plan, true, out var newPlan);
        Enum.TryParse<Currency>(request.Currency, true, out var newCurrency);
        
        var command = new UpdateSubscriptionCommand(
            UserId: userId,
            SubscriptionId: id,
            Name: request.Name,
            Plan: newPlan,
            CustomPeriodInDays: request.CustomPeriodInDays,
            TrialPeriodInDays: request.TrialPeriodInDays,
            StartDate: request.StartDate,
            Cost: request.Cost,
            Currency: newCurrency,
            Note: request.Note,
            CancelLink: request.CancelLink,
            PicLink: request.PicLink
        );

        var result = await mediator.Send(command, cancellationToken);
        
        return result;
    }
}