using System.Net.Mime;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RenuMate.Api.Common;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Users.Reactivate;

public abstract class ReactivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPatch("api/users/me", Handle)
        .WithSummary("Reactivates user account.")
        .WithDescription("Reactivates a deactivated user account using a valid reactivation token.")
        .WithTags("Users")
        .RequireAuthorization()
        .Produces<TokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

    private static async Task<IResult> Handle(
        [FromBody] ReactivateUserRequest request,
        IValidator<ReactivateUserRequest> validator,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var command = new ReactivateUserCommand(request.Token);
        var result = await mediator.Send(command, cancellationToken);

        return result;
    }
}
