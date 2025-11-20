using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ResetPassword;

public abstract class ResetPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/reset-password", Handle)
        .WithSummary("Resets a user's password.")
        .WithDescription("Resets the user's password using a valid password reset token and returns a new access token upon success.")
        .WithTags("Authentication")
        .Produces<TokenResponse>(200, "application/json")
        .Produces(400)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromQuery] string token,
        [FromBody] ResetPasswordRequest request, 
        [FromServices] ITokenService tokenService,
        [FromServices] IConfiguration configuration,
        [FromServices] RenuMateDbContext db,
        [FromServices] IPasswordHasher passwordHasher,
        [FromServices] ILogger<ResetPasswordEndpoint> logger,
        [FromServices] IValidator<ResetPasswordRequest> requestValidator,
        [FromServices] IValidator<string> tokenValidator,
        CancellationToken cancellationToken = default)
    {
        var requestValidation = await requestValidator.ValidateAsync(request, cancellationToken);
        
        if (!requestValidation.IsValid)
        {
            return requestValidation.ToFailureResult();
        }
        
        var tokenValidation = await requestValidator.ValidateAsync(request, cancellationToken);
        
        if (!tokenValidation.IsValid)
        {
            return tokenValidation.ToFailureResult();
        }
        
        var principal = tokenService.ValidateToken(token, expectedPurpose: "ConfirmEmail");
        
        if (principal is null)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid or expired token",
                detail: "The password reset token is invalid or has expired."
            );
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The token does not contain a valid user identifier."
            );
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "No user exists for the given token."
            );
        }
        
        var newHashedPassword = passwordHasher.HashPassword(request.NewPassword);
        
        try
        {
            user.PasswordHash = newHashedPassword;
            await db.SaveChangesAsync(cancellationToken);
            
            var accessToken = tokenService.CreateToken(
                userId: stringUserId,
                email: user.Email,
                purpose: "Access",
                emailConfirmed: user.IsEmailConfirmed ? "true" : "false",
                expiresAt: DateTime.UtcNow.AddHours(24));
            
            return Results.Ok(new TokenResponse
            {
                Token = accessToken
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while resetting password for user {UserId}.", user.Id);

            return Results.Problem(
                statusCode: 500,
                title: "Internal server error",
                detail: "An unexpected error occurred while resetting the password."
            );
        }
    }
}