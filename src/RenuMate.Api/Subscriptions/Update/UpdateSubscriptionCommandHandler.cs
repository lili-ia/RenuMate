using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Subscriptions.Update;

public class UpdateSubscriptionCommandHandler(RenuMateDbContext db, TimeProvider timeProvider) 
    : IRequestHandler<UpdateSubscriptionCommand, IResult>
{
    public async Task<IResult> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.UserId == request.UserId, cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Subscription not found."
            );
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        
        try
        {
            subscription.UpdateDetails(request.Name, request.Note, request.CancelLink, request.PicLink);
            subscription.ChangePricing(request.Cost, request.Currency);
            subscription.UpdatePlanAndStartDate(
                request.Plan, request.StartDate, now, request.CustomPeriodInDays, request.TrialPeriodInDays);
            
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
                statusCode: StatusCodes.Status409Conflict,
                title: "Subscription with this name already exists.",
                detail: "You can not create more than one subscription with similar names."
            );
        }
    }
}