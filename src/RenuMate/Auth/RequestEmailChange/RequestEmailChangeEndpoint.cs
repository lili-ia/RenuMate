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
        IEmailTemplateService emailTemplateService,
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
            return Results.Problem(
                statusCode: 401,
                title: "Unauthorized",
                detail: "User authentication context is missing."
            );
        }
        
        logger.LogInformation("User {UserId} requested email change to {NewEmail}", userId, request.NewEmail);
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Results.Problem(
                statusCode: 404,
                title: "User not found",
                detail: "The requested user does not exist."
            );
        }
        
        var emailExists = await db.Users
            .AnyAsync(u => u.Email == request.NewEmail, cancellationToken);
        
        if (emailExists)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Email already in use",
                detail: "The provided email address is already registered."
            );
        }
        
        if (string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: 400,
                title: "Invalid email",
                detail: "The new email must be different from the current email address."
            );
        }
        
        var token = tokenService.CreateToken(
            userId: userId.ToString(),
            email: request.NewEmail,
            purpose: "ChangeEmail",
            emailConfirmed: "true",
            expiresAt: DateTime.UtcNow.AddHours(24));
        
        var frontendUrl = configuration["App:FrontendUrl"];

        var confirmLink = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
        var body = emailTemplateService.BuildEmailChangeConfirmationMessage(
            user.Name, 
            request.NewEmail, 
            confirmLink);
        
        var sent = await emailSender.SendEmailAsync(
            request.NewEmail, 
            "Confirm your new email", 
            body);
        
        if (!sent)
        {
            return Results.Problem(
                statusCode: 500,
                title: "Email sending failure",
                detail: "Failed to send the email change confirmation email. Please try again later."
            );
        }
        
        return Results.Ok(new MessageResponse
        {
            Message = "Email change request sent successfully. Please check your new email to confirm the change."
        });
    }
}

