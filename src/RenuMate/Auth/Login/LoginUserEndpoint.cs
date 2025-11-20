using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.Login;

public abstract class LoginUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/login", Handle)
        .WithSummary("Logs a user in.")
        .WithDescription("Validates user credentials and returns an access token if successful.")
        .WithTags("Authentication")
        .Produces<TokenResponse>(200, "application/json")
        .Produces(400)
        .Produces(401);

    private static async Task<IResult> Handle(
        [FromBody] LoginUserRequest request,
        RenuMateDbContext db,
        ILogger<LoginUserEndpoint> logger,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration,
        IValidator<LoginUserRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Login attempt failed for non-existent email: {Email}", request.Email);
            
            return Results.Problem(
                statusCode: 401,
                title: "Authentication failed",
                detail: "Invalid email or password."
            );
        }
        
        var passwordValid = passwordHasher.VerifyHashedPassword(request.Password, user.PasswordHash);
        
        if (!passwordValid)
        {
            logger.LogWarning("Login attempt failed for user {UserId}: Invalid password.", user.Id);
            
            return Results.Problem(
                statusCode: 401,
                title: "Authentication failed",
                detail: "Invalid email or password."
            );
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login attempt for deactivated user {UserId}.", user.Id);
            
            return Results.Problem(
                statusCode: 401,
                title: "Account deactivated",
                detail: "Your account is deactivated. Please reactivate to log in."
            );
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "Access",
            emailConfirmed: user.IsEmailConfirmed ? "true" : "false",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        logger.LogInformation("User {UserId} logged in successfully.", user.Id);
        
        return Results.Ok(new TokenResponse
        {
            Token = token
        });
    }
}