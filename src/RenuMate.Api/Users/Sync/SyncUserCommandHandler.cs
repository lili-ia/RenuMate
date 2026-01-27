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
            var updated = user.UpdateProfile(request.Email, request.Name, request.IsVerified);

            if (updated)
            {
                await db.SaveChangesAsync(cancellationToken);
            
                logger.LogInformation("Existing user {UserId} with auth0 id {Auth0Id} was successfully synced with database.", 
                    user.Id, request.Auth0Id);
            
                return Results.Ok(new SyncUserResponse ("User successfully synced.", user.Id));
            }

            return Results.Ok(new SyncUserResponse("User already synced.", user.Id));
        }

        var newUser = User.Create(request.Auth0Id, request.Email, request.Name);

        if (request.IsVerified)
        {
            newUser.ConfirmEmail();
        }
        
        db.Users.Add(newUser);
        
        await auth0Service.UpdateUserInternalIdAsync(newUser.Auth0Id, newUser.Id, cancellationToken);

        newUser.MarkMetadataAsSynced();
        await db.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("New user {UserId} with auth0 id {AuthId} successfully created in database and synced with Auth0.", 
            newUser.Id, request.Auth0Id);
        
        return Results.Ok(new SyncUserResponse ("User created", newUser.Id));
    }
}