using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Common;
using RenuMate.Api.Entities;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.RequestReactivate;

public abstract class RequestUserReactivateEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/reactivate-request", Handle)
        .RequireAuthorization()
        .WithSummary("Request user account reactivation.")
        .WithDescription("If the account exists and is deactivated, a reactivation email is sent with a verification link.")
        .WithTags("Users")
        .Produces<MessageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

    private const string Message = "If your account exists and is deactivated, a reactivation email was sent."; 
    
    private static async Task<IResult> Handle(
        IUserContext userContext,
        RenuMateDbContext db,
        ITokenService tokenService,
        IConfiguration configuration,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        ILogger<RequestUserReactivateEndpoint> logger,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User {UserId} was authorized but not found in database.", userId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: "The authenticated user could not be found in the database."
            );
        }

        if (user.IsActive)
        {
            logger.LogWarning("User {UserId} requested an account reactivation but is already active.", userId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "User already active",
                detail: "The authenticated user is already active."
            );
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
            
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Reactivation pending",
                detail: "We couldn't send the activation link. We will try to send it automatically soon. " +
                        "Please check your email in a few minutes."
            );
        }
        
        logger.LogInformation("User with email {Email} successfully requested to reactivate their account.", user.Email);

        return Results.Ok(new MessageResponse(Message));
    }
}