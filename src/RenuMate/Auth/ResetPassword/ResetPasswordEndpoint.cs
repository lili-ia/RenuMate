using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ResetPassword;

public class ResetPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/reset-password", Handle)
        .WithSummary("Resets password.");

    public static async Task<Result<ResetPasswordResponse>> Handle(
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
            return requestValidation.ToFailureResult<ResetPasswordResponse>();
        }
        
        var tokenValidation = await requestValidator.ValidateAsync(request, cancellationToken);
        
        if (!tokenValidation.IsValid)
        {
            return tokenValidation.ToFailureResult<ResetPasswordResponse>();
        }
        
        var principal = tokenService.ValidateToken(token, expectedPurpose: "ConfirmEmail");
        
        if (principal == null)
        {
            return Result<ResetPasswordResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
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
            
            var accessToken = tokenService.CreateToken(
                userId: user.Id.ToString(),
                email: user.Email,
                purpose: "Access",
                expiresAt: DateTime.UtcNow.AddHours(24));
            
            return Result<ResetPasswordResponse>.Success(new ResetPasswordResponse
            {
                Token = accessToken
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