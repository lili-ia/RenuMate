using System.Net.Mime;
using System.Security.Claims;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Entities;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Extensions;
using Entities_User = RenuMate.Api.Entities.User;
using User = RenuMate.Api.Entities.User;

namespace RenuMate.Api.Users.Sync;

public class SyncUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/sync-user", Handle)
        .RequireAuthorization()
        .WithSummary("Syncs Auth0 user with local database")
        .WithDescription("Creates a new user or links an existing legacy user by email.")
        .Produces<SyncUserResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

    private static async Task<Results<Ok<SyncUserResponse>, ProblemHttpResult>> Handle(
        ClaimsPrincipal claimsUser,
        RenuMateDbContext db,
        ILogger<SyncUserEndpoint> logger, 
        IConfiguration config,
        IManagementApiClient managementClient,
        IAuth0Service auth0Service,
        CancellationToken ct = default)
    {
        var (auth0Id, _, email, name, isVerified) = claimsUser.GetUserInfo(config);

        if (string.IsNullOrEmpty(auth0Id) || string.IsNullOrEmpty(email))
        {
            return TypedResults.Problem(statusCode: 400, title: "Invalid Identity Claims");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id, ct);

        if (user is not null)
        {
            user.UpdateProfile(email, name);
            await EnsureMetadataSynced(user, auth0Service, db, ct);
            
            return TypedResults.Ok(new SyncUserResponse ("User synced", user.Id));
        }

        user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is not null)
        {
            user.LinkAuth0Account(auth0Id, isVerified);
            await EnsureMetadataSynced(user, auth0Service, db, ct);
            
            return TypedResults.Ok(new SyncUserResponse ("Legacy user linked", user.Id));
        }

        var newUser = User.Create(auth0Id, email, name);

        if (isVerified)
        {
            newUser.ConfirmEmail();
        }
        
        db.Users.Add(newUser);
        await EnsureMetadataSynced(newUser, auth0Service, db, ct);
        
        return TypedResults.Ok(new SyncUserResponse ("User created", newUser.Id));
    }
    
    private static async Task EnsureMetadataSynced(
        Entities_User user, 
        IAuth0Service auth0Service, 
        RenuMateDbContext db, 
        CancellationToken ct)
    {
        if (user.IsMetadataSynced) 
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        await auth0Service.UpdateUserInternalIdAsync(user.Auth0Id, user.Id, ct);
        user.MarkMetadataAsSynced();
    
        await db.SaveChangesAsync(ct);
    }
}