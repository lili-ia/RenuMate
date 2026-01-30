using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Tags.Delete;

public class DeleteTagEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/tags/{id:guid}", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .RequireAuthorization("VerifiedEmailOnly")
        .WithSummary("Deletes the tag with the specified ID for the authenticated user.")
        .WithTags("Tags")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        RenuMateDbContext db,
        IUserContext userContext,
        ILogger<DeleteTagEndpoint> logger,
        IMemoryCache cache,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var tag = await db.Tags
            .FirstOrDefaultAsync(t => t.Id == id && (t.UserId == userId || t.IsSystem), cancellationToken);

        if (tag is null)
        {
            logger.LogInformation("Tag {TagId} not found by user {UserId}.", id, userId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Tag not found",
                detail: "No tag exists with the specified ID for the current user."
            );
        }

        if (tag.IsSystem)
        {
            logger.LogInformation("User {UserId} attempted to delete a system tag {TagId}.", userId, id);
            
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Tag is reserved by system",
                detail: "You can not delete a tag that is reserved by system."
            );
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} successfully deleted tag {TagId}", userId, id);
        
        var cacheKey = $"tags_{userId}";
        cache.Remove(cacheKey);

        return Results.NoContent();
    }
}