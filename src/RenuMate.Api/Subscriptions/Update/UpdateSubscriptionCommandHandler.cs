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
        var requestedTags = await db.Tags
            .Where(t => request.TagIds.Contains(t.Id) && (t.UserId == request.UserId || t.IsSystem))
            .ToListAsync(cancellationToken);
        
        if (requestedTags.Count != request.TagIds.Distinct().Count())
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid tags",
                detail: "One or more tag IDs are invalid or don't belong to you."
            );
        }
        
        var subscription = await db.Subscriptions
            .Include(s => s.Tags)
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
            subscription.UpdateTags(requestedTags);
            
            await db.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Subscription {SubId} was successfully updated by user {UserId}.", 
                request.SubscriptionId, request.UserId);
            
            return Results.Ok(new UpdateSubscriptionResponse
            (
                subscription.Id,
                subscription.Name,
                subscription.RenewalDate,
                $"{subscription.Cost}{subscription.Currency}",
                TagIds: request.TagIds,
                subscription.Note,
                subscription.CancelLink,
                subscription.PicLink
            ));
        }
        catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException { SqlState: "23505" } npgsqlEx)
        {
            var message = npgsqlEx.Message;
    
            if (message.Contains("IX_Subscriptions_Name") || message.Contains("Name"))
            {
                logger.LogInformation("Conflict: Subscription name '{Name}' already exists for user {UserId}.", 
                    request.Name, request.UserId);

                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Subscription name conflict",
                    detail: "You already have a subscription with this name."
                );
            }

            if (message.Contains("SubscriptionTag") || message.Contains("PK_SubscriptionTag"))
            {
                logger.LogWarning("Conflict: Duplicate tags detected for subscription {SubId}.", request.SubscriptionId);

                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Tag conflict",
                    detail: "The subscription already contains one of these tags."
                );
            }

            logger.LogWarning("Database conflict (23505): {Message}", message);
    
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Data conflict",
                detail: "A record with these details already exists."
            );
        }
    }
}