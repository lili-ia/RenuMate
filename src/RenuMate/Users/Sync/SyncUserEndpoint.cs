using System.Net.Mime;
using System.Security.Claims;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;
using User = RenuMate.Entities.User;

namespace RenuMate.Users.Sync;

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
            var changed = false;
            
            if (user.Email != email || user.Name != name)
            {
                user.Email = email;
                user.Name = name;
                changed = true;
            }

            if (!user.IsMetadataSynced)
            {
                await auth0Service.UpdateUserInternalIdAsync(auth0Id, user.Id, ct);
                user.IsMetadataSynced = true;
                changed = true;
            }

            if (changed)
            {
                await db.SaveChangesAsync(ct);
            }
            
            return TypedResults.Ok(new SyncUserResponse ("User synced", user.Id));
        }

        user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is not null)
        {
            if (!string.IsNullOrEmpty(user.Auth0Id))
            {
                return TypedResults.Problem(statusCode: 409, title: "Account Already Linked");
            }
            
            user.Auth0Id = auth0Id;
            user.EmailConfirmed = isVerified;
            
            await auth0Service.UpdateUserInternalIdAsync(auth0Id, user.Id, ct);
            user.IsMetadataSynced = true;
            
            await db.SaveChangesAsync(ct);
            
            return TypedResults.Ok(new SyncUserResponse ("Legacy user linked", user.Id));
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Auth0Id = auth0Id,
            Email = email,
            Name = name,
            EmailConfirmed = isVerified,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsMetadataSynced = false
        };

        try
        {
            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct); 

            await auth0Service.UpdateUserInternalIdAsync(auth0Id, newUser.Id, ct);
            
            newUser.IsMetadataSynced = true;
            await db.SaveChangesAsync(ct);
            
            return TypedResults.Ok(new SyncUserResponse ("User created", newUser.Id));
        }
        catch (DbUpdateException)
        {
            return TypedResults.Problem(statusCode: 409, title: "User Already Exists");
        }
    }
}