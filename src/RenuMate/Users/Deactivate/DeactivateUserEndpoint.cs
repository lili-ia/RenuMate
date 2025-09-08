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

    public static async Task<Result<DeactivateUserResponse>> Handle(
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Result<DeactivateUserResponse>.Failure("Unauthorized.", ErrorType.Unauthorized);
        }
        
        try
        {
            var rows = await db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsActive, false), cancellationToken);
            
            if (rows == 0)
            {
                return Result<DeactivateUserResponse>.Failure("User not found.", ErrorType.NotFound);
            }
         
            return Result<DeactivateUserResponse>.Success(new DeactivateUserResponse
            {
                Message = "Your account was successfully deactivated."
            });
        }
        catch (Exception ex)
        {
            return Result<DeactivateUserResponse>.Failure("An internal error occurred.", ErrorType.ServerError);
        }
    }
}