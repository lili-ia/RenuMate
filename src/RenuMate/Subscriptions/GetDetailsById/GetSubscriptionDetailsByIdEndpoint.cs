using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.DTOs;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.GetDetailsById;

public abstract class GetSubscriptionDetailsByIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Get subscription details by ID.")
        .WithDescription("Returns detailed information about a subscription owned by the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<SubscriptionDetailsDto>(200, "application/json")
        .Produces(401)
        .Produces(404);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        IUserContext userContext,
        RenuMateDbContext db,
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

        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == id && s.UserId == userId)
            .Select(SubscriptionMapper.ProjectToDetailsDto)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return Results.Problem(
                statusCode: 404,
                title: "Subscription not found",
                detail: "No subscription exists with the specified ID for the current user."
            );
        }

        return Results.Ok(subscription);
    }
}