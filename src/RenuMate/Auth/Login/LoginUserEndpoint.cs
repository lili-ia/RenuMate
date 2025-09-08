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

    public static async Task<Result<LoginUserResponse>> Handle(
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
            return validation.ToFailureResult<LoginUserResponse>();
        }
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
        {
            return Result<LoginUserResponse>.Failure("Invalid email or password.", ErrorType.Unauthorized);
        }
        
        var passwordValid = passwordHasher.VerifyHashedPassword(request.Password, user.PasswordHash);
        
        if (!passwordValid)
        {
            return Result<LoginUserResponse>.Failure("Invalid email or password.", ErrorType.Unauthorized);
        }

        if (!user.IsActive)
        {
            return Result<LoginUserResponse>.Failure(
                "Your account is deactivated. Please reactivate to log in.", ErrorType.Unauthorized 
            );
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "Access",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        return Result<LoginUserResponse>.Success(new LoginUserResponse
        {
            AccessToken = token
        });
    }
}

public class LoginUserResponse
{
    public string AccessToken { get; set; }
}