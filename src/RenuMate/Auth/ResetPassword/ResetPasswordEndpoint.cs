using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ResetPassword;

public class ResetPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("/api/auth/reset-password", Handle)
        .WithSummary("Resets password.");

    public static async Task<Result<ResetPasswordResponse>> Handle(
        ResetPasswordRequest request, 
        ITokenService tokenService,
        IConfiguration configuration,
        RenuMateDbContext db,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordEndpoint> logger,
        IValidator<ResetPasswordRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult<ResetPasswordResponse>();
        }
        
        var principal = tokenService.ValidateToken(request.Token, expectedPurpose: "ConfirmEmail");
        
        if (principal == null)
        {
            return Result<ResetPasswordResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
        }

        var stringUserId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Result<ResetPasswordResponse>.Failure("Invalid token.", ErrorType.BadRequest);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<ResetPasswordResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
        }
        
        var newHashedPassword = passwordHasher.HashPassword(request.NewPassword);
        
        try
        {
            user.PasswordHash = newHashedPassword;
            await db.SaveChangesAsync(cancellationToken);
            
            var token = tokenService.CreateToken(
                userId: user.Id.ToString(),
                email: user.Email,
                purpose: "Access",
                expiresAt: DateTime.UtcNow.AddHours(24));
            
            return Result<ResetPasswordResponse>.Success(new ResetPasswordResponse
            {
                Token = token
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while resetting password for user {UserId}.", user.Id);

            return Result<ResetPasswordResponse>.Failure("Invalid or expired token.");
        }
    }
}

public class ResetPasswordResponse
{
    public string Token { get; set; } = null!;
}