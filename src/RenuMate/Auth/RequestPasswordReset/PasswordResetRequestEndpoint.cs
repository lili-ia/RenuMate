using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.RequestPasswordReset;

public abstract class PasswordResetRequestEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/recover-password-request", Handle)
        .WithSummary("Request password recovery.")
        .WithDescription("Sends a password reset link to the provided email address if the account exists.")
        .WithTags("Authentication")
        .Produces<MessageResponse>(200, "application/json")
        .Produces(400)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromBody] PasswordResetRequest request,
        [FromServices] RenuMateDbContext db,
        [FromServices] IConfiguration configuration,
        [FromServices] IEmailSender emailSender,
        [FromServices] ITokenService tokenService,
        [FromServices] IValidator<PasswordResetRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user is null)
        {
            return Results.Ok(new MessageResponse
            {
                Message = "If an account exists, a password reset email was sent."
            });
        }
        
        var signingKey = configuration["Jwt:SigningKey"];

        if (signingKey is null)
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "PasswordRecover",
            emailConfirmed: user.IsEmailConfirmed ? "true" : "false",
            expiresAt: DateTime.UtcNow.AddMinutes(30));
        
        var frontendUrl = configuration["App:FrontendUrl"];
        
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("Frontend Url is not configured.");
        }
        
        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var body = $"<p>Click the link below to reset your password:</p>" +
                   $"<p><a href='{resetLink}'>Reset Password</a></p>";
        
        var sentSuccess = await emailSender.SendEmailAsync(user.Email, "Password Reset", body);
        
        if (!sentSuccess)
        {
            return Results.InternalServerError();
        }
        
        return Results.Ok(new MessageResponse
        {
            Message = "If an account exists, a password reset email was sent."
        });
    }
}