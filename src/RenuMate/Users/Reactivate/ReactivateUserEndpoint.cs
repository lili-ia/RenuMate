using System.Net.Mime;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.Reactivate;

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
        [FromQuery] string token,
        IValidator<string> validator,
        ITokenService tokenService,
        RenuMateDbContext db,
        ILogger<ReactivateUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(token, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var principal = tokenService.ValidateToken(token, expectedPurpose: "Reactivate");
        
        if (principal == null)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The reactivation token is invalid or has expired."
            );
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The reactivation token is invalid or has expired."
            );
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The reactivation token is invalid or has expired."
            );
        }
        
        user.Activate();
        
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.CreateToken(
            userId: userId.ToString(), 
            email: user.Email, 
            purpose: "Reactivate",
            emailConfirmed: "true",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        return TypedResults.Ok(new TokenResponse
        (
            Token: accessToken
        ));
    }
}
