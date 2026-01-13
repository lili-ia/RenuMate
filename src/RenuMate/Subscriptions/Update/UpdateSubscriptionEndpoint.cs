using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        .RequireAuthorization("VerifiedEmailOnly")
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
        
        Enum.TryParse<SubscriptionPlan>(request.Plan, true, out var type);
        Enum.TryParse<Currency>(request.Currency, true, out var currency);

        subscription.Name = request.Name;
        subscription.Plan = type;
        subscription.CustomPeriodInDays = request.CustomPeriodInDays;
        subscription.StartDate = request.StartDate;
        subscription.Cost = request.Cost;
        subscription.Currency = currency;
        subscription.Note = request.Note;
        subscription.CancelLink = request.CancelLink;
        subscription.PicLink = request.PicLink;
        
        try
        {
            subscription.UpdateNextRenewalDate();
            
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
            logger.LogError(ex, "Error while updating subscription {SubscriptionId}.", id);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while updating the subscription."
            );
        }
    }
}