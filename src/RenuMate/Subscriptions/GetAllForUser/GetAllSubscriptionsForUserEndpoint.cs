using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.DTOs;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Subscriptions.GetAllForUser;

public abstract class GetAllSubscriptionsForUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions", Handle)
        .RequireAuthorization("EmailConfirmed")
        .WithSummary("Get user subscriptions.")
        .WithDescription("Returns a paginated list of subscriptions belonging to the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<PaginatedResponse<SubscriptionDto>>(200, "application/json")
        .Produces(401);
    
    private static async Task<IResult> Handle(
        [FromServices] IUserContext userContext,
        [FromServices] RenuMateDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
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
            .Select(SubscriptionMapper.ProjectToDto)
            .ToListAsync(cancellationToken);

        var result = new PaginatedResponse<SubscriptionDto>
        {
            Items = subscriptions,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Results.Ok(result);
    }
}