using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Auth.Register;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ResendEmailConfirmation;

public abstract class ResendEmailConfirmationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/resend-email-confirmation", Handle)
        .WithSummary("Resends email confirmation.")
        .WithDescription("Resends a confirmation email to users who have not yet verified their email address.")
        .WithTags("Authentication")
        .Produces<MessageResponse>(200, "application/json")
        .Produces(400)
        .Produces(404)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromBody] ResendEmailConfirmationRequest request,
        RenuMateDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ITokenService tokenService,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        IValidator<ResendEmailConfirmationRequest> validator,
        ILogger<RegisterUserEndpoint> logger,
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
            return Results.Problem(
                statusCode: 404,
                title: "User not found",
                detail: "No account exists with the provided email."
            );
        }
        
        if (user.IsEmailConfirmed)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Email already confirmed",
                detail: "This email address has already been confirmed."
            );
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "ConfirmEmail",
            emailConfirmed: "false",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        var frontendUrl = configuration["App:FrontendUrl"];
        
        var confirmLink = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
        var body = emailTemplateService.BuildConfirmEmailMessage(confirmLink);

        var sent = await emailSender.SendEmailAsync(request.Email, "Confirm your email", body);
        
        if (!sent)
        {
            return Results.Problem(
                statusCode: 500,
                title: "Email sending failed",
                detail: "Failed to send the confirmation email. Please try again later."
            );
        }
        
        return Results.Ok(new MessageResponse
        {
            Message = "Account created successfully. Please check your email to verify your account."
        });
    }
}