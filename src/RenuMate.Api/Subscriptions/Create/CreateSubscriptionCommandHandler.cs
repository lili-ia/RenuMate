using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RenuMate.Api.Entities;
using RenuMate.Api.Enums;
using RenuMate.Api.Persistence;

namespace RenuMate.Api.Subscriptions.Create;

public class CreateSubscriptionCommandHandler(RenuMateDbContext db, TimeProvider timeProvider) 
    : IRequestHandler<CreateSubscriptionCommand, IResult>
{
    public async Task<IResult> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        
        try
        {
            var subscription = request.Plan switch
            {
                SubscriptionPlan.Trial => Subscription.CreateTrial(
                    request.Name, request.TrialPeriodInDays ?? 7, request.StartDate, request.UserId, request.Cost, 
                    request.Currency, now, cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note),
                
                SubscriptionPlan.Custom => Subscription.CreateCustom(
                    request.Name, request.CustomPeriodInDays ?? 30, request.Cost, request.Currency, request.StartDate,
                    request.UserId, now, cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note),

                _ => Subscription.CreateStandard(
                    request.Name, request.Plan, request.Cost, request.Currency, request.StartDate, request.UserId,
                    now, cancelLink: request.CancelLink, picLink: request.PicLink, note: request.Note)
            };

            db.Subscriptions.Add(subscription); 
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new CreateSubscriptionResponse(
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
                title: "Subscription already exists.",
                detail: "You cannot create more than one subscription with the same name."
            );
        }
    }
}