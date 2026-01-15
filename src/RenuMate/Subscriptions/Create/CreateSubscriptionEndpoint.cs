using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Middleware;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Create;

public abstract class CreateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/subscriptions", Handle)
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
        RenuMateDbContext db,
        ILogger<CreateSubscriptionEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        Enum.TryParse<SubscriptionPlan>(request.Plan, true, out var plan);
        Enum.TryParse<Currency>(request.Currency, true, out var currency);
        
        try
        {
            var subscription = plan switch
            {
                SubscriptionPlan.Trial => Subscription.CreateTrial(
                    request.Name, request.TrialPeriodInDays ?? 7, userId, request.Cost, currency, 
                    cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note),
                
                SubscriptionPlan.Custom => Subscription.CreateCustom(
                    request.Name, request.CustomPeriodInDays ?? 30, request.Cost, currency, request.StartDate, userId,
                    cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note),

                _ => Subscription.CreateStandard(
                    request.Name, plan, request.Cost, currency, request.StartDate, userId,
                    cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note)
            };

            db.Subscriptions.Add(subscription); 
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new CreateSubscriptionResponse(
                subscription.Id,
                subscription.Name,
                subscription.RenewalDate,
                $"{subscription.Cost}{subscription.Currency}",
                subscription.Note,
                subscription.CancelLink,
                subscription.PicLink
            ));
        }
        catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException { SqlState: "23505" })  
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Subscription already exists.",
                detail: "You cannot create more than one subscription with the same name."
            );
        }
    }
}