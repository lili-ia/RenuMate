using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.RequestEmailChange;

public abstract class RequestEmailChangeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/change-email", Handle)
        .RequireAuthorization()
        .WithSummary("Request email change.")
        .WithDescription("Sends a verification link to the new email address for confirmation.")
        .WithTags("Authentication")
        .Produces<MessageResponse>(200, "application/json")
        .Produces(400) 
        .Produces(401) 
        .Produces(404)
        .Produces(500);
    
    private static async Task<IResult> Handle(
        [FromBody] EmailChangeRequest request,
        RenuMateDbContext db,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<RequestEmailChangeEndpoint> logger,
        IUserContext userContext,
        IValidator<EmailChangeRequest> validator,
        IEmailSender emailSender,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }
        
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }
        
        logger.LogInformation("User {UserId} requested email change to {NewEmail}", userId, request.NewEmail);
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Results.NotFound(new { Error = "User not found." });
        }
        
        var emailExists = await db.Users
            .AnyAsync(u => u.Email == request.NewEmail, cancellationToken);
        
        if (emailExists)
        {
            return Results.BadRequest(new { Error = "Email is already in use." });
        }
        
        if (string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { Error = "New email must be different from the current one." });
        
        var token = tokenService.CreateToken(
            userId: userId.ToString(),
            email: request.NewEmail,
            purpose: "ChangeEmail",
            emailConfirmed: "true",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        var frontendUrl = configuration["App:FrontendUrl"];

        var confirmLink = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
        
        var body = $"""
                        <p>Hello {user.Name},</p>
                        <p>You requested to change your email address to <b>{request.NewEmail}</b>.</p>
                        <p>Please confirm your new email by clicking the link below:</p>
                        <p><a href="{confirmLink}">Confirm Email Change</a></p>
                        <p>This link will expire in 24 hours.</p>
                        <p>If you didn’t request this change, you can safely ignore this email.</p>
                    """;
        
        var sentSuccess = await emailSender.SendEmailAsync(request.NewEmail, "Confirm your new email", body);
        
        if (!sentSuccess)
        {
            return Results.InternalServerError();
        }
        
        return Results.Ok(new MessageResponse
        {
            Message = "Email change request sent successfully. Please check your new email to confirm the change."
        });
    }
}

