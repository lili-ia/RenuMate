using System.Net.Mime;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.DTOs;
using RenuMate.Api.Extensions;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Subscriptions.GetAllForUser;

public abstract class GetAllSubscriptionsForUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/subscriptions", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Get user subscriptions.")
        .WithDescription("Returns a paginated list of subscriptions belonging to the authenticated user.")
        .WithTags("Subscriptions")
        .Produces<PaginatedResponse<SubscriptionDetailsDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized);
    
    private static async Task<IResult> Handle(
        [AsParameters] GetAllSubscriptionsRequest request,
        IValidator<GetAllSubscriptionsRequest> validator,
        IUserContext userContext,
        RenuMateDbContext db,
        ILogger<GetAllSubscriptionsForUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var query = db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(s => s.Name) 
                : query.OrderBy(s => s.Name),
            
            "renewaldate" => request.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(s => s.RenewalDate) 
                : query.OrderBy(s => s.RenewalDate),
            
            _ => query.OrderByDescending(s => s.CreatedAt) 
        };
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var subscriptions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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
        
        logger.LogInformation("User {UserId} successfully retrieved {Count} subscriptions.", userId, subscriptions.Count);

        return Results.Ok(result);
    }
}