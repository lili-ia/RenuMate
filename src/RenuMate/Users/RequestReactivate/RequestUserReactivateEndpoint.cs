using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.RequestReactivate;

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

    private static async Task<IResult> Handle(
        [FromBody] ReactivateUserRequest userRequest, 
        RenuMateDbContext db,
        ITokenService tokenService,
        IConfiguration configuration,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        IValidator<ReactivateUserRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(userRequest, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult();
        }
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userRequest.Email, cancellationToken);

        if (user is null || user.IsActive)
        {
            return Results.Ok(new MessageResponse
            (
                Message: "If your account exists and is deactivated, a reactivation email was sent."
            ));
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

        var sent =  await emailSender.SendEmailAsync(user.Email, "Reactivate your account", body);

        if (!sent)
        {
            return Results.Problem(
                statusCode: 500,
                title: "Email Sending Failed",
                detail: "Could not send the reactivation email."
            );
        }

        return Results.Ok(new MessageResponse
        (
            Message: "If your account exists and is deactivated, a reactivation email was sent."
        ));
    }
}