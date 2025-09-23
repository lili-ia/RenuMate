using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Create;

public class CreateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("api/subscriptions", Handle);

    private static async Task<IResult> Handle(
        [FromBody] CreateSubscriptionRequest request,
        [FromServices] IUserContext userContext,
        [FromServices] IValidator<CreateSubscriptionRequest> validator,
        [FromServices] RenuMateDbContext db,
        [FromServices] ILogger<CreateSubscriptionEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
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
        
        var renewalDate = new DateTime();
        switch (plan)
        {
            case SubscriptionPlan.Monthly:
                renewalDate = request.StartDate.AddMonths(1);
                break;
            case SubscriptionPlan.Quarterly:
                renewalDate = request.StartDate.AddMonths(3);
                break;
            case SubscriptionPlan.Annual:
                renewalDate = request.StartDate.AddYears(1);
                break;
            case SubscriptionPlan.Custom when request.CustomPeriodInDays.HasValue:
                renewalDate = request.StartDate.AddDays(request.CustomPeriodInDays.Value);
                break;
        }

        subscription.RenewalDate = renewalDate;
        
        try
        {
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating subscription for user {UserId}.", userId);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}