using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.GetActiveStatus;

public class GetUserActiveStatusEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/users/status", Handle)
        .RequireAuthorization()
        .WithSummary("Get user activation status")
        .WithDescription("Checks if the current authenticated user's account is marked as active in the database.")
        .WithTags("Users")
        .Produces<UserStatusResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status401Unauthorized);

    private static async Task<IResult> Handle(
        IUserContext userContext,
        RenuMateDbContext db,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var isActive = await db.Users
            .AnyAsync(u => u.Id == userId && u.IsActive == true, cancellationToken);

        return Results.Ok(new UserStatusResponse(isActive));
    }
}