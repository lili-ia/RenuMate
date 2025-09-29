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
        .WithSummary("Registers a new user.");

    private static async Task<IResult> Handle(
        [FromBody] RegisterUserRequest request,
        RenuMateDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ITokenService tokenService,
        IEmailSender emailSender,
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
            return Results.Conflict("User with this email already registered.");
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while registering user with email {Email}", request.Email);

            return Results.InternalServerError("An internal error occurred.");
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "ConfirmEmail",
            emailConfirmed: "false",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        var frontendUrl = configuration["App:FrontendUrl"];

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("Frontend Url is not configured.");
        }
        
        var confirmLink = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
        
        var body = $"<p>Please confirm your email by clicking the link below:</p>" +
                   $"<p><a href='{confirmLink}'>Confirm Email</a></p>";

        await emailSender.SendEmailAsync(request.Email, "Confirm your email", body);
        
        return Results.Ok(new RegisterUserResponse
        {
            Message = "Account created successfully. Please check your email to verify your account."
        });
    }
}

public class RegisterUserResponse
{
    public string Message { get; set; }
}