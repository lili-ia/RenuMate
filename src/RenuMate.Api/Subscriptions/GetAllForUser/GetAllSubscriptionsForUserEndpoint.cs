using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.DTOs;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.GetAllForUser;

public abstract class GetAllSubscriptionsForUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions", Handle)
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Get user subscriptions.")
        .WithDescription("Returns a paginated list of subscriptions belonging to the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<PaginatedResponse<SubscriptionDetailsDto>>(200, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized);
    
    private static async Task<IResult> Handle(
        [FromServices] IUserContext userContext,
        [FromServices] RenuMateDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var totalCount = await db.Subscriptions
            .Where(s => s.UserId == userId)
            .CountAsync(cancellationToken);

        page = page < 1 ? 1 : page; 
        pageSize = pageSize < 1 ? 10 : pageSize; 
        pageSize = pageSize > 100 ? 100 : pageSize;
        
        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.StartDate) 
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SubscriptionMapper.ProjectToDetailsDto)
            .ToListAsync(cancellationToken);

        var result = new PaginatedResponse<SubscriptionDetailsDto>
        (
            Items: subscriptions,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(result);
    }
}