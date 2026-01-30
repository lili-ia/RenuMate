using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Common;
using RenuMate.Api.DTOs;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Tags.GetAll;

public class GetAllTagsForUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/tags", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Gets all tags for user including common system tags.")
        .WithTags("Tags")
        .Produces<List<TagDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> Handle(
        RenuMateDbContext db,
        IUserContext userContext,
        ILogger<GetAllTagsForUserEndpoint> logger,
        IMemoryCache cache,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var cacheKey = $"tags_{userId}";

        if (!cache.TryGetValue(cacheKey, out List<TagDto>? tags))
        {
            tags = await db.Tags
                .AsNoTracking()
                .Where(t => t.IsSystem || t.UserId == userId)
                .OrderByDescending(t => t.IsSystem)
                .ThenBy(t => t.Name)
                .Select(t => new TagDto(
                    t.Id, 
                    t.Name, 
                    t.Color, 
                    t.IsSystem))
                .ToListAsync(cancellationToken);
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            cache.Set(cacheKey, tags, cacheOptions);
        }

        return Results.Ok(tags);
    }
}