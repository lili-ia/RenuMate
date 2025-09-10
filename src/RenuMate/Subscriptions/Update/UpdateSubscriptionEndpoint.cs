using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Common;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Update;

public class UpdateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPut("api/subscriptions/{id:guid}", Handle);

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromBody] UpdateSubscriptionRequest request,
        [FromServices] IUserContext userContext,
        [FromServices] IValidator<UpdateSubscriptionRequest> validator,
        [FromServices] RenuMateDbContext db,
        [FromServices] ILogger<UpdateSubscriptionEndpoint> logger,
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

        var subscription = await db.Subscriptions.FindAsync([id], cancellationToken);

        if (subscription is null)
        {
            return Results.NotFound("Subscription not found.");
        }

        if (subscription.UserId != userId)
        {
            return Results.Forbid();
        }
        
        Enum.TryParse<SubscriptionType>(request.Type, true, out var type);
        Enum.TryParse<Currency>(request.Currency, true, out var currency);
        
        var renewalDate = new DateTime();
        switch (type)
        {
            case SubscriptionType.Monthly:
                renewalDate = request.StartDate.AddMonths(1);
                break;
            case SubscriptionType.Quarterly:
                renewalDate = request.StartDate.AddMonths(3);
                break;
            case SubscriptionType.Annual:
                renewalDate = request.StartDate.AddYears(1);
                break;
            case SubscriptionType.Custom when request.CustomPeriodInDays.HasValue:
                renewalDate = request.StartDate.AddDays(request.CustomPeriodInDays.Value);
                break;
        }

        subscription.Name = request.Name;
        subscription.Type = type;
        subscription.CustomPeriodInDays = request.CustomPeriodInDays;
        subscription.StartDate = request.StartDate;
        subscription.RenewalDate = renewalDate;
        subscription.Cost = request.Cost;
        subscription.Currency = currency;
        subscription.Note = request.Note;
        
        try
        {
            await db.SaveChangesAsync(cancellationToken); 
            
            return Results.Ok(new UpdateSubscriptionResponse
            {
                Id = subscription.Id,
                Name = subscription.Name,
                RenewalDate = subscription.RenewalDate,
                Cost = $"{subscription.Cost}{subscription.Currency}",
                Note = subscription.Note
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while updating subscription {SubscriptionId}.", id);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}