using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.Reactivate;

public class ReactivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/reactivate", Handle)
        .WithSummary("Reactivates user account.");

    public static async Task<Result<ReactivateUserResponse>> Handle(
        [FromQuery] string token,
        [FromServices] IValidator<string> validator,
        [FromServices] ITokenService tokenService,
        [FromServices] RenuMateDbContext db,
        [FromServices] ILogger<ReactivateUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(token, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult<ReactivateUserResponse>();
        }

        var principal = tokenService.ValidateToken(token, expectedPurpose: "Reactivate");
        
        if (principal == null)
        {
            return Result<ReactivateUserResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Result<ReactivateUserResponse>.Failure("Invalid token.", ErrorType.BadRequest);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<ReactivateUserResponse>.Failure("Invalid or expired token.", 
                ErrorType.BadRequest);
        }

        if (user.IsActive)
        {
            return Result<ReactivateUserResponse>.Failure("Your account is already active.", 
                ErrorType.Conflict);
        }
        
        user.IsActive = true;
        
        try
        {
            await db.SaveChangesAsync(cancellationToken);

            var accessToken = tokenService.CreateToken(
                userId: userId.ToString(), 
                email: user.Email, 
                purpose: "Reactivate",
                expiresAt: DateTime.UtcNow.AddHours(24));
            
            return Result<ReactivateUserResponse>.Success(new ReactivateUserResponse
            {
                Token = accessToken
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reactivating user with {UserId}", user.Id);
            
            return Result<ReactivateUserResponse>.Failure("Invalid or expired token.");
        }
    }
}

public class ReactivateUserResponse
{
    public string Token { get; set; }
}