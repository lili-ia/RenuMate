using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Create;

public abstract class CreateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/subscriptions", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Create a subscription.")
        .WithDescription("Creates a new subscription for the authenticated user, calculating the renewal date based on the plan and optional custom period.")
        .WithTags("Subscriptions")
        .Produces<CreateSubscriptionResponse>(200, "application/json")
        .Produces(400)
        .Produces(401)
        .Produces(500);
    
    private static async Task<IResult> Handle(
        [FromBody] CreateSubscriptionRequest request,
        IUserContext userContext,
        IValidator<CreateSubscriptionRequest> validator,
        RenuMateDbContext db,
        ILogger<CreateSubscriptionEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: 401,
                title: "Unauthorized",
                detail: "User is not authenticated."
            );
        }
        
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
            StartDate = request.StartDate,
            Cost = request.Cost,
            Currency = currency,
            IsMuted = false,
            Note = request.Note,
            CancelLink = request.CancelLink,
            UserId = userId
        };
        
        try
        {
            var renewalDate = plan switch
            {
                SubscriptionPlan.Monthly => request.StartDate.AddMonths(1),
                SubscriptionPlan.Quarterly => request.StartDate.AddMonths(3),
                SubscriptionPlan.Annual => request.StartDate.AddYears(1),
                SubscriptionPlan.Custom when request.CustomPeriodInDays.HasValue => request.StartDate.AddDays(
                    request.CustomPeriodInDays.Value),
                _ => new DateTime()
            };

            subscription.RenewalDate = renewalDate;
            
            await db.Subscriptions.AddAsync(subscription, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new CreateSubscriptionResponse
            {
                Id = subscription.Id,
                Name = subscription.Name,
                RenewalDate = subscription.RenewalDate,
                Cost = $"{subscription.Cost}{subscription.Currency}",
                Note = subscription.Note,
                CancelLink = subscription.CancelLink,
                PicLink = subscription.PicLink
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid request",
                detail: ex.Message
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating subscription for user {UserId}.", userId);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while creating the subscription."
            );
        }
    }
}