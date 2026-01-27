using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenuMate.Api.Common;
using RenuMate.Api.Entities;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Users.RequestReactivate;

public class RequestReactivateUserCommandHandler(RenuMateDbContext db,
    ITokenService tokenService,
    IConfiguration configuration,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    ILogger<RequestReactivateUserCommandHandler> logger,
    TimeProvider timeProvider,
    IMemoryCache cache) : IRequestHandler<RequestReactivateUserCommand, IResult>
{
    private const string Message = "If your account exists and is deactivated, a reactivation email was sent."; 
    
    public async Task<IResult> Handle(RequestReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = $"cooldown_{request.UserId}";

        if (cache.TryGetValue(cacheKey, out _))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Request cooldown active", 
                detail: "Please wait before requesting another link.");
        }
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User {UserId} was authorized but not found in database.", request.UserId);
            
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: "The authenticated user could not be found in the database."
            );
        }

        if (user.IsActive)
        {
            logger.LogWarning("User {UserId} requested an account reactivation but is already active.", request.UserId);
            
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
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));

        cache.Set(cacheKey, true, cacheOptions);
        
        logger.LogInformation("User with email {Email} successfully requested to reactivate their account.", user.Email);

        return Results.Ok(new MessageResponse(Message));
    }
}