using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ConfirmEmail;

public class ConfirmEmailEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/auth/confirm-email", Handle)
        .WithSummary("Confirms email.");
    
    public static async Task<Result<ConfirmEmailResponse>> Handle(
        [FromQuery] string token,
        [FromServices] RenuMateDbContext db,
        [FromServices] ITokenService tokenService,
        [FromServices] ILogger<ConfirmEmailEndpoint> logger,
        [FromServices] IValidator<string> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(token, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult<ConfirmEmailResponse>();
        }

        var principal = tokenService.ValidateToken(token, expectedPurpose: "ConfirmEmail");
        
        if (principal == null)
        {
            return Result<ConfirmEmailResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Result<ConfirmEmailResponse>.Failure("Invalid token.", ErrorType.BadRequest);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.IsEmailConfirmed)
        {
            return Result<ConfirmEmailResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
        }

        user.IsEmailConfirmed = true;
        
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            
            var accessToken = tokenService.CreateToken(
                userId: user.Id.ToString(),
                email: user.Email,
                purpose: "Access",
                expiresAt: DateTime.UtcNow.AddHours(24));
            
            return Result<ConfirmEmailResponse>.Success(new ConfirmEmailResponse
            {
                Message = "Email confirmed successfully.",
                Token = accessToken
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while confirming email for user {UserId}", userId);
            
            return Result<ConfirmEmailResponse>.Failure("Invalid or expired token.");
        }
    }
}

public class ConfirmEmailResponse
{
    public string Message { get; set; } = null!;

    public string Token { get; set; } = null!;
}