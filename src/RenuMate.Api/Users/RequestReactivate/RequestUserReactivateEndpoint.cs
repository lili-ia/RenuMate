using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Entities;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Extensions;

namespace RenuMate.Api.Users.RequestReactivate;

public abstract class RequestUserReactivateEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/reactivate-request", Handle)
        .WithSummary("Request user account reactivation.")
        .WithDescription("If the account exists and is deactivated, a reactivation email is sent with a verification link.")
        .WithTags("Users")
        .Produces<MessageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

    private const string Message = "If your account exists and is deactivated, a reactivation email was sent."; 
    
    private static async Task<IResult> Handle(
        [FromBody] ReactivateUserRequest request, 
        RenuMateDbContext db,
        ITokenService tokenService,
        IConfiguration configuration,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        ILogger<RequestUserReactivateEndpoint> logger,
        IValidator<ReactivateUserRequest> validator,
        TimeProvider timeProvider,
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

        if (user is null || user.IsActive)
        {
            logger.LogInformation("User with email {Email} requested to reactivate their account but {Reason}.",
                request.Email, user is null ? "was not found" : "is already active");
            
            return Results.Ok(new MessageResponse(Message));
        }

        var frontendUrl = configuration["App:FrontendUrl"];

        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "Reactivate",
            emailConfirmed: "true",
            expiresAt: DateTime.UtcNow.AddHours(1));

        var link = $"{frontendUrl}/reactivate?token={Uri.EscapeDataString(token)}";
        var body = emailTemplateService.BuildUserReactivateMessage(user.Name, link);

        var emailSenderResponse = await emailSender.SendEmailAsync(
            user.Email, "Reactivate your account", body, cancellationToken);

        if (!emailSenderResponse.IsSuccess)
        {
            logger.LogError("Failed to send reactivation email to {Email}", user.Email);
            
            var pendingEmail = PendingEmail.Create(
                to: user.Email, 
                subject: "Reactivate your account", 
                body,
                now: timeProvider.GetUtcNow().UtcDateTime,
                lastError: emailSenderResponse.ErrorMessage);
            
            await db.PendingEmails.AddAsync(pendingEmail, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        
        logger.LogInformation("User with email {Email} successfully requested to reactivate their account.", user.Email);

        return Results.Ok(new MessageResponse(Message));
    }
}