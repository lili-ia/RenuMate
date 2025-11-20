using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Common;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Update;

public abstract class UpdateSubscriptionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPut("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Update subscription.")
        .WithDescription("Updates the details of a subscription owned by the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<UpdateSubscriptionResponse>(200, "application/json")
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromBody] UpdateSubscriptionRequest request,
        IUserContext userContext,
        IValidator<UpdateSubscriptionRequest> validator,
        RenuMateDbContext db,
        ILogger<UpdateSubscriptionEndpoint> logger,
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

        var subscription = await db.Subscriptions.FindAsync([id], cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: 403,
                title: "Forbidden",
                detail: "You do not have permission to update this subscription."
            );
        }

        if (subscription.UserId != userId)
        {
            return Results.Forbid();
        }
        
        Enum.TryParse<SubscriptionPlan>(request.Plan, true, out var type);
        Enum.TryParse<Currency>(request.Currency, true, out var currency);

        var renewalDate = type switch
        {
            SubscriptionPlan.Monthly => request.StartDate.AddMonths(1),
            SubscriptionPlan.Quarterly => request.StartDate.AddMonths(3),
            SubscriptionPlan.Annual => request.StartDate.AddYears(1),
            SubscriptionPlan.Custom when request.CustomPeriodInDays.HasValue => request.StartDate.AddDays(
                request.CustomPeriodInDays.Value),
            _ => new DateTime()
        };

        subscription.Name = request.Name;
        subscription.Plan = type;
        subscription.CustomPeriodInDays = request.CustomPeriodInDays;
        subscription.StartDate = request.StartDate;
        subscription.RenewalDate = renewalDate;
        subscription.Cost = request.Cost;
        subscription.Currency = currency;
        subscription.Note = request.Note;
        subscription.CancelLink = request.CancelLink;
        subscription.PicLink = request.PicLink;
        
        try
        {
            await db.SaveChangesAsync(cancellationToken); 
            
            return Results.Ok(new UpdateSubscriptionResponse
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
            logger.LogError(ex, "Error while updating subscription {SubscriptionId}.", id);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while updating the subscription."
            );
        }
    }
}