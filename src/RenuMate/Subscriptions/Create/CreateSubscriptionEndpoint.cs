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
        .WithDescription("Creates a new subscription for the authenticated user, calculating the renewal date based on the plan and optional custom period.")
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

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Name = request.Name,
            Plan = plan,
            CustomPeriodInDays = request.CustomPeriodInDays,
            TrialPeriodInDays = request.TrialPeriodInDays,
            StartDate = request.StartDate,
            Cost = request.Cost,
            Currency = currency,
            IsMuted = false,
            Note = request.Note,
            CancelLink = request.CancelLink,
            PicLink = request.PicLink,
            UserId = userId
        };
          
        try
        {
            subscription.UpdateNextRenewalDate(isInitialization: true);

            await db.Subscriptions.AddAsync(subscription, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new CreateSubscriptionResponse
            (
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
                title: "Subscription with this name already exists.",
                detail: "You can not create more than one subscription with similar names."
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: ex.Message
            );
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: ex.Message
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating subscription for user {UserId}.", userId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: "An unexpected error occurred while creating the subscription."
            );
        }
    }
}