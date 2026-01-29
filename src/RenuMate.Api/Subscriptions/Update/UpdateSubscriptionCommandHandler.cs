using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Subscriptions.Update;

public class UpdateSubscriptionCommandHandler(
    RenuMateDbContext db, 
    TimeProvider timeProvider, 
    ILogger<UpdateSubscriptionCommandHandler> logger) 
    : IRequestHandler<UpdateSubscriptionCommand, IResult>
{
    public async Task<IResult> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.UserId == request.UserId, cancellationToken);

        if (subscription is null)
        {
            logger.LogInformation("Subscription {SubId} not found by user {UserId}.", request.SubscriptionId, request.UserId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Subscription not found."
            );
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        
        try
        {
            subscription.UpdateDetails(request.Name, request.Note, request.CancelLink, request.PicLink);
            subscription.ChangePricing(request.Cost, request.Currency);
            subscription.UpdatePlanAndStartDate(
                request.Plan, request.StartDate, today, request.CustomPeriodInDays, request.TrialPeriodInDays);
            
            await db.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Subscription {SubId} was successfully updated by user {UserId}.", 
                request.SubscriptionId, request.UserId);
            
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
            logger.LogInformation("User {UserId} attempted to create more than one subscription with the same name.", 
                request.UserId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Subscription with this name already exists.",
                detail: "You can not create more than one subscription with similar names."
            );
        }
    }
}