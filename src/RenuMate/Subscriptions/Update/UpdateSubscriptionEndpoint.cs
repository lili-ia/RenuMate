using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Common;
using RenuMate.Enums;
using RenuMate.Extensions;
using RenuMate.Middleware;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.Update;

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
        RenuMateDbContext db,
        ILogger<UpdateSubscriptionEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
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
        
        Enum.TryParse<SubscriptionPlan>(request.Plan, true, out var newPlan);
        Enum.TryParse<Currency>(request.Currency, true, out var newCurrency);

        try
        {
            subscription.UpdateDetails(request.Name, request.Note, request.CancelLink, request.PicLink);
            subscription.ChangePricing(request.Cost, newCurrency);

            if (subscription.StartDate != request.StartDate)
            {
                subscription.RescheduleStartDate(request.StartDate);
            }

            if (subscription.Plan != newPlan)
            {
                subscription.ChangePlan(newPlan, request.CustomPeriodInDays);
            }

            await db.SaveChangesAsync(cancellationToken);
            
            return Results.Ok(new UpdateSubscriptionResponse
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
    }
}