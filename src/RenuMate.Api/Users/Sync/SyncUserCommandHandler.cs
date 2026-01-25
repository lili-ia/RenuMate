using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Entities;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.Sync;

public class SyncUserCommandHandler(RenuMateDbContext db, IAuth0Service auth0Service, ILogger<SyncUserCommandHandler> logger) 
    : IRequestHandler<SyncUserCommand, IResult>
{
    public async Task<IResult> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Auth0Id == request.Auth0Id, cancellationToken);

        if (user is not null)
        {
            user.UpdateProfile(request.Email, request.Name);
            await EnsureMetadataSynced(user, auth0Service, db, cancellationToken);
            
            return Results.Ok(new SyncUserResponse ("User successfully synced.", user.Id));
        }

        var newUser = User.Create(request.Auth0Id, request.Email, request.Name);

        if (request.IsVerified)
        {
            newUser.ConfirmEmail();
        }
        
        db.Users.Add(newUser);
        await EnsureMetadataSynced(newUser, auth0Service, db, cancellationToken);
        
        return Results.Ok(new SyncUserResponse ("User created", newUser.Id));
    }
    
    private static async Task EnsureMetadataSynced(
        User user,
        IAuth0Service auth0Service,
        RenuMateDbContext db,
        CancellationToken ct)
    {
        if (user.IsMetadataSynced)
        {
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            await auth0Service.UpdateUserInternalIdAsync(user.Auth0Id, user.Id, ct);

            user.MarkMetadataAsSynced();

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            await db.Users.ExecuteUpdateAsync(setters
                => setters.SetProperty(u => u.IsMetadataSynced, false), ct);
            throw;
        }
    }

}