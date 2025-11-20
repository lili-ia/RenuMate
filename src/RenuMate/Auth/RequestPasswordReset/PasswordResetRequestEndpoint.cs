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
        RenuMateDbContext db,
        IConfiguration configuration,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        ITokenService tokenService,
        IValidator<PasswordResetRequest> validator,
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
            return Results.Ok(new MessageResponse
            {
                Message = "If an account exists, a password reset email was sent."
            });
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "PasswordRecover",
            emailConfirmed: user.IsEmailConfirmed ? "true" : "false",
            expiresAt: DateTime.UtcNow.AddMinutes(30));
        
        var frontendUrl = configuration["App:FrontendUrl"];
        
        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        var body = emailTemplateService.BuildPasswordResetMessage(user.Name, resetLink);
        
        var sent = await emailSender.SendEmailAsync(user.Email, "Password Reset", body);
        
        if (!sent)
        {
            return Results.Problem(
                statusCode: 500,
                title: "Email sending failure",
                detail: "Failed to send password reset email. Please try again later."
            );
        }
        
        return Results.Ok(new MessageResponse
        {
            Message = "If an account exists, a password reset email was sent."
        });
    }
}