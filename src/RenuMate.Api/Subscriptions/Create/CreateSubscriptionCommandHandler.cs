using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Api.Entities;
using RenuMate.Api.Enums;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Subscriptions.Create;

public class CreateSubscriptionCommandHandler(
    RenuMateDbContext db, 
    TimeProvider timeProvider, 
    ILogger<CreateSubscriptionCommandHandler> logger) 
    : IRequestHandler<CreateSubscriptionCommand, IResult>
{
    public async Task<IResult> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
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
        
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        
        try
        {
            var subscription = request.Plan switch
            {
                SubscriptionPlan.Trial => Subscription.CreateTrial(
                    request.Name, request.TrialPeriodInDays ?? 7, request.StartDate, request.UserId, request.Cost, 
                    request.Currency, today, cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note),
                
                SubscriptionPlan.Custom => Subscription.CreateCustom(
                    request.Name, request.CustomPeriodInDays ?? 30, request.Cost, request.Currency, request.StartDate,
                    request.UserId, today, cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note),

                _ => Subscription.CreateStandard(
                    request.Name, request.Plan, request.Cost, request.Currency, request.StartDate, request.UserId,
                    today, cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note)
            };

            foreach (var tag in requestedTags)
            {
                subscription.AddTag(tag);
            }

            db.Subscriptions.Add(subscription); 
            await db.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("User {UserId} successfully created new subscription {SubId}.", request.UserId, subscription.Id);

            return Results.Created(
                $"/api/subscriptions/{subscription.Id}", 
                new CreateSubscriptionResponse(
                subscription.Id,
                subscription.Name,
                subscription.RenewalDate,
                subscription.Plan,
                $"{subscription.Cost}{subscription.Currency}",
                TagIds: request.TagIds,
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
                title: "Subscription already exists.",
                detail: "You cannot create more than one subscription with the same name."
            );
        }
    }
}