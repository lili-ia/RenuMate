using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ConfirmEmail;

public abstract class ConfirmEmailEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/confirm-email", Handle)
        .WithSummary("Confirms a user's email address.")
        .WithDescription("Validates the email confirmation token, marks the user's email as confirmed, and returns an access token.")
        .WithTags("Authentication")
        .Produces<TokenResponse>(200, "application/json")
        .Produces(400);

    private static async Task<IResult> Handle(
        [FromBody] ConfirmEmailRequest request,
        [FromServices] RenuMateDbContext db,
        [FromServices] ITokenService tokenService,
        [FromServices] ILogger<ConfirmEmailEndpoint> logger,
        [FromServices] IValidator<string> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request.Token, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }

        var principal = tokenService.ValidateToken(request.Token, expectedPurpose: "ConfirmEmail");
        
        if (principal == null)
        {
            return Results.BadRequest("Invalid or expired token.");
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Results.BadRequest("Invalid or expired token.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.IsEmailConfirmed)
        {
            return Results.BadRequest("Invalid or expired token.");
        }

        user.IsEmailConfirmed = true;
        
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            
            var accessToken = tokenService.CreateToken(
                userId: user.Id.ToString(),
                email: user.Email,
                purpose: "Access",
                emailConfirmed: "true",
                expiresAt: DateTime.UtcNow.AddHours(24));
            
            return Results.Ok(new TokenResponse
            {
                Token = accessToken
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while confirming email for user {UserId}", userId);
            
            return Results.BadRequest("Invalid or expired token.");
        }
    }
}