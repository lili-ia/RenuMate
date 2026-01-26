using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.Deactivate;

public abstract class DeactivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/users/me", Handle)
        .RequireAuthorization()
        .WithSummary("Deactivate user account.")
        .WithDescription("Sets the authenticated user's account to inactive.")
        .WithTags("Users")
        .Produces<MessageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> Handle(
        RenuMateDbContext db,
        IUserContext userContext,
        IAuth0Service auth0Service,
        ILogger<DeactivateUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var user = await db.Users
            .Include(u => u.Subscriptions)
                .ThenInclude(s => s.Reminders)
                    .ThenInclude(r => r.ReminderOccurrences)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User {UserId} was authorized but not found in database.", userId);
            
            return Results.Problem(
                statusCode: 404,
                title: "User not found",
                detail: "The authenticated user could not be found."
            );
        }
        
        if (!user.IsActive)
        {
            logger.LogWarning("User {UserId} attempted to deactivate their account but is already inactive.", userId);
            
            return Results.Ok(new MessageResponse("User account is already deactivated."));
        }
        
        user.Deactivate();
        await auth0Service.SetUserActiveStatusAsync(user.Auth0Id, isActive: false, cancellationToken);
        
        await db.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} successfully deactivated their account, data was synced with Auth0.", userId);

        return Results.Ok(new MessageResponse("Your account was successfully deactivated."));
    }
}