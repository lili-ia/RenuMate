using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.Deactivate;

public abstract class DeactivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/users", Handle)
        .RequireAuthorization()
        .WithSummary("Deactivate user account.")
        .WithDescription("Sets the authenticated user's account to inactive.")
        .WithTags("Users")
        .Produces<MessageResponse>(200, "application/json")
        .Produces(401)
        .Produces(404)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        [FromServices] ILogger<DeactivateUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }
        
        try
        {
            var rows = await db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsActive, false), cancellationToken);
            
            if (rows == 0)
            {
                return Results.NotFound("User not found.");
            }
         
            return Results.Ok(new MessageResponse
            {
                Message = "Your account was successfully deactivated."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deactivating user {UserId}.", userId);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}