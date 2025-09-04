using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.Deactivate;

public class DeactivateUserEndpoint : EndpointWithoutRequest<Result<DeactivateUserResponse>>
{
    private readonly IUserContext _userContext;
    private readonly RenuMateDbContext _db;
    
    public DeactivateUserEndpoint(IUserContext userContext, RenuMateDbContext db)
    {
        _userContext = userContext;
        _db = db;
    }

    public override void Configure()
    {
        Roles("User");
        Delete("/api/users");
    }

    public override async Task<Result<DeactivateUserResponse>> HandleAsync(CancellationToken ct)
    {
        var userId = _userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Result<DeactivateUserResponse>.Failure("Unauthorized.", ErrorType.Unauthorized);
        }
        
        try
        {
            var rows = await _db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsActive, false), ct);
            
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