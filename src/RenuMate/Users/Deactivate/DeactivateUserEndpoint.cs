using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.Deactivate;

public class DeactivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapDelete("api/users", Handle)
        .WithSummary("Deactivates user account.")
        .RequireAuthorization();

    private static async Task<IResult> Handle(
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
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
         
            return Results.Ok(new DeactivateUserResponse
            {
                Message = "Your account was successfully deactivated."
            });
        }
        catch (Exception ex)
        {
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}

public class DeactivateUserResponse
{
    public string Message { get; set; } = null!;
}