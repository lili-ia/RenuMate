using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.Reactivate;

public class ReactivateUserCommandHandler(
    ITokenService tokenService, 
    RenuMateDbContext db, 
    IAuth0Service auth0Service,
    ILogger<ReactivateUserCommandHandler> logger) 
    : IRequestHandler<ReactivateUserCommand, IResult>
{
    public async Task<IResult> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.ValidateToken(request.Token, expectedPurpose: "Reactivate");
        
        if (principal is null)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The reactivation token is invalid or has expired."
            );
        }

        var stringUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(stringUserId, out var userId))
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The reactivation token is invalid or has expired."
            );
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User {UserId} was authorized but not found in database.", userId);
            
            return Results.Problem(
                statusCode: 400,
                title: "Invalid token",
                detail: "The reactivation token is invalid or has expired."
            );
        }
        
        user.Activate();
        await auth0Service.SetUserActiveStatusAsync(user.Auth0Id, isActive: true, cancellationToken);
        
        await db.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} successfully activated their account, data was synced with Auth0.", userId);

        var accessToken = tokenService.CreateToken(
            userId: userId.ToString(), 
            email: user.Email, 
            purpose: "Reactivate",
            emailConfirmed: "true",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        return Results.Ok(new TokenResponse(accessToken));
    }
}