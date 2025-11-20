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
        RenuMateDbContext db,
        ITokenService tokenService,
        ILogger<ConfirmEmailEndpoint> logger,
        IValidator<string> validator,
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
            return TokenProblem("The provided confirmation token is invalid or has expired.");
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return TokenProblem("The token contains an invalid user identifier.");
        }
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Email confirmation attempt failed for user {UserId}: User does not exist in db.", userId);
            
            return TokenProblem("No user exists for the provided token.");
        }

        if (user.IsEmailConfirmed)
        {
            logger.LogWarning("Email confirmation attempt failed for user {UserId}: Email already confirmed.", userId);
            
            return TokenProblem("The email address has already been confirmed.");
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
            
            logger.LogInformation("User {UserId} successfully confirmed their email.", userId);
            
            return Results.Ok(new TokenResponse
            {
                Token = accessToken
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while confirming email for user {UserId}.", userId);
            
            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while confirming the email."
            );
        }
    }

    private static IResult TokenProblem(string detail = "Invalid or expired token") =>
        Results.Problem(
            statusCode: 400,
            title: "Invalid token",
            detail: detail
        );
}