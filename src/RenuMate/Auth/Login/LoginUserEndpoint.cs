using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.Login;

public class LoginUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/login", Handle)
        .WithSummary("Logs new user in.");

    private static async Task<IResult> Handle(
        [FromBody] LoginUserRequest request,
        [FromServices] RenuMateDbContext db,
        [FromServices] IPasswordHasher passwordHasher,
        [FromServices] ITokenService tokenService,
        [FromServices] IConfiguration configuration,
        [FromServices] IValidator<LoginUserRequest> validator,
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
            return Results.Problem(detail: "Invalid email or password.", statusCode: 401);
        }
        
        var passwordValid = passwordHasher.VerifyHashedPassword(request.Password, user.PasswordHash);
        
        if (!passwordValid)
        {
            return Results.Problem(detail: "Invalid email or password.", statusCode: 401);
        }

        if (!user.IsActive)
        {
            return Results.Problem(
                detail: "Your account is deactivated. Please reactivate to log in.", statusCode: 401);
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "Access",
            emailConfirmed: user.IsEmailConfirmed ? "true" : "false",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        return Results.Ok(new LoginUserResponse
        {
            AccessToken = token
        });
    }
}

public class LoginUserResponse
{
    public string AccessToken { get; set; }
}