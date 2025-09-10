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

    public async static Task<IResult> Handle(
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

        Enum.TryParse<SubscriptionType>(request.Type, true, out var type);
        Enum.TryParse<Currency>(request.Currency, true, out var currency);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Name = request.Name,
            Type = type,
            CustomPeriodInDays = request.CustomPeriodInDays,
            StartDate = request.StartDate,
            Cost = request.Cost,
            Currency = currency,
            Note = request.Note,
            UserId = userId
        };
        
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

        subscription.RenewalDate = renewalDate;

        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            SubscriptionId = subscription.Id,
            DaysBeforeRenewal = 1,
            NotifyTime = new TimeSpan(9, 0, 0),
            IsMuted = false
        };
        
        try
        {
            await db.Subscriptions.AddAsync(subscription, cancellationToken);
            await db.Reminders.AddAsync(reminder, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new CreateSubscriptionResponse
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
            logger.LogError(ex, "Error while creating subscription for user {UserId}.", userId);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}