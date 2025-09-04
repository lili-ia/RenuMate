using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.Delete;

public class DeleteUserEndpoint : EndpointWithoutRequest<Result>
{
    private readonly IUserContext _userContext;
    private readonly RenuMateDbContext _db;
    
    public DeleteUserEndpoint(IUserContext userContext, RenuMateDbContext db)
    {
        _userContext = userContext;
        _db = db;
    }

    public override void Configure()
    {
        Roles("User");
        Delete("/api/users");
    }

    public override async Task<Result> HandleAsync(CancellationToken ct)
    {
        var userId = _userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Result.Failure("Unauthorized.", ErrorType.Unauthorized);
        }
        
        try
        {
            var rows = await _db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(u => u.IsActive, false), ct);
            
            if (rows == 0)
            {
                return Result.Failure("User not found.", ErrorType.NotFound);
            }
         
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("An internal error occurred.", ErrorType.ServerError);
        }
    }
}