using System.Net.Mime;
using System.Security.Claims;
using MediatR;
using RenuMate.Api.Common;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Users.Sync;

public class SyncUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/sync-user", Handle)
        .RequireAuthorization("ActiveUserOnly")
        .WithSummary("Syncs Auth0 user with local database")
        .WithDescription("Creates a new user or links an existing legacy user by email.")
        .Produces<SyncUserResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status502BadGateway);
    
    private static async Task<IResult> Handle(
        ClaimsPrincipal claimsUser,
        IMediator mediator,
        IConfiguration config,
        CancellationToken ct = default) 
    {
        var (auth0Id, _, email, name, isVerified) = claimsUser.GetUserInfo(config);

        if (string.IsNullOrEmpty(auth0Id) || string.IsNullOrEmpty(email))
        {
            return TypedResults.Problem(statusCode: 400, title: "Invalid Identity Claims");
        }

        var command = new SyncUserCommand(
            Auth0Id: auth0Id, 
            Email: email, 
            Name: name, 
            IsVerified: isVerified);
        
        var result = await mediator.Send(command, ct);

        return result;
    }
}