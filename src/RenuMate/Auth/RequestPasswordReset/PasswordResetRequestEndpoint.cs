using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.RequestPasswordReset;

public class PasswordResetRequestEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/recover-password-request", Handle)
        .WithSummary("Requests a password recover for user.");

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
            return Results.Ok(new PasswordResetRequestResponse
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
        
        await emailSender.SendEmailAsync(user.Email, "Password Reset", body);
        
        return Results.Ok(new PasswordResetRequestResponse
        {
            Message = "If an account exists, a password reset email was sent."
        });
    }
}

public class PasswordResetRequestResponse
{
    public string Message { get; set; }
}