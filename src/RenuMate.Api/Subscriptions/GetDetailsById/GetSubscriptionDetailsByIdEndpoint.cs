using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.DTOs;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.GetDetailsById;

public abstract class GetSubscriptionDetailsByIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions/{id:guid}", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Get subscription details by ID.")
        .WithDescription("Returns detailed information about a subscription owned by the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<SubscriptionDetailsDto>(200, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);
    
    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        IUserContext userContext,
        RenuMateDbContext db,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
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