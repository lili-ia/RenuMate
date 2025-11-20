using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.Register;

public abstract class RegisterUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/register", Handle)
        .WithSummary("Registers a new user.")
        .WithDescription("Creates a new user account, hashes the password, and sends an email confirmation link.")
        .WithTags("Authentication")
        .Produces<MessageResponse>(200, "application/json")
        .Produces(400)
        .Produces(409)
        .Produces(500);

    private static async Task<IResult> Handle(
        [FromBody] RegisterUserRequest request,
        RenuMateDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ITokenService tokenService,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        IValidator<RegisterUserRequest> validator,
        ILogger<RegisterUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }
        
        var userExists = await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (userExists)
        {
            logger.LogWarning("Registration attempt for existing email: {Email}", request.Email);
            
            return Results.Problem(
                statusCode: 409,
                title: "Email already registered",
                detail: "A user with this email already exists."
            );
        }

        var hashedPassword = passwordHasher.HashPassword(request.Password);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Email = request.Email,
            Name = request.Name,
            PasswordHash = hashedPassword,
            IsEmailConfirmed = false,
            IsActive = true
        };

        try
        {
            await db.Users.AddAsync(user, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("User {UserId} registered successfully.", user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while registering user with email {Email}", request.Email);

            logger.LogError("Frontend URL is not configured.");
            return Results.Problem(
                statusCode: 500,
                title: "Server error",
                detail: "Internal server error."
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

        if (sent)
        {
            return Results.Ok(new MessageResponse
            {
                Message = "Account created successfully. Please check your email to verify your account."
            });
        }
            
        logger.LogError("Failed to send confirmation email to {Email}", request.Email);
        
        return Results.Problem(
            statusCode: 500,
            title: "Email delivery failed",
            detail: "Could not send confirmation email. Please try again later."
        );
    }
}