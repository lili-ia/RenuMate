using System.Net.Mime;
using MediatR;
using RenuMate.Api.Common;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.RequestReactivate;

public abstract class RequestUserReactivateEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/reactivate-request", Handle)
        .RequireAuthorization()
        .WithSummary("Request user account reactivation.")
        .WithDescription("If the account exists and is deactivated, a reactivation email is sent with a verification link.")
        .WithTags("Users")
        .Produces<MessageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);
    
    private static async Task<IResult> Handle(
        IUserContext userContext,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        var command = new RequestReactivateUserCommand(userId);
        var result = await mediator.Send(command, cancellationToken);

        return result;
    }
}