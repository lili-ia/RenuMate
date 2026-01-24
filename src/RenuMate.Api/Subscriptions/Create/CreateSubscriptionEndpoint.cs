using System.Net.Mime;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Api.Common;
using RenuMate.Api.Enums;
using RenuMate.Api.Middleware;
using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Subscriptions.Create;

public abstract class CreateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/subscriptions", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .AddEndpointFilter<InvalidateSummaryCacheEndpointFilter>()
        .WithSummary("Create a subscription.")
        .WithDescription("Creates a new subscription for the authenticated user, calculating the renewal date based " +
                         "on the plan and optional custom period.")
        .WithTags("Subscriptions")
        .Produces<CreateSubscriptionResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);
    
    private static async Task<IResult> Handle(
        [FromBody] CreateSubscriptionRequest request,
        IUserContext userContext,
        IValidator<CreateSubscriptionRequest> validator,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        Enum.TryParse<SubscriptionPlan>(request.Plan, true, out var plan); // both enums are already validated in corresponding validators
        Enum.TryParse<Currency>(request.Currency, true, out var currency);

        var command = new CreateSubscriptionCommand(
            UserId: userId,
            Name: request.Name,
            Plan: plan,
            CustomPeriodInDays: request.CustomPeriodInDays,
            TrialPeriodInDays: request.TrialPeriodInDays,
            StartDate: request.StartDate,
            Cost: request.Cost,
            Currency: currency,
            Note: request.Note,
            CancelLink: request.CancelLink,
            PicLink: request.PicLink
        );

        var result = await mediator.Send(command, cancellationToken);

        return result;
    }
}