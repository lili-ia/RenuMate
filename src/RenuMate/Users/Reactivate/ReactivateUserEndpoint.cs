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

    private static async Task<IResult> Handle(
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
            return validation.ToFailureResult();
        }

        var principal = tokenService.ValidateToken(token, expectedPurpose: "Reactivate");
        
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

        if (user is null)
        {
            return Results.BadRequest("Invalid or expired token.");
        }

        if (user.IsActive)
        {
            return Results.Conflict("Your account is already active.");
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
            
            return Results.Ok(new ReactivateUserResponse
            {
                Token = accessToken
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reactivating user with {UserId}", user.Id);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}

public class ReactivateUserResponse
{
    public string Token { get; set; }
}