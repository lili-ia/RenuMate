using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.RequestReactivate;

public class RequestUserReactivateEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/users/reactivate-request", Handle)
        .WithSummary("Requests user account reactivation.");

    public static async Task<Result<ReactivateRequestResponse>> Handle(
        [FromBody] ReactivateRequest request, 
        [FromServices] RenuMateDbContext db,
        [FromServices] ITokenService tokenService,
        [FromServices] IConfiguration configuration,
        [FromServices] IEmailSender emailSender,
        [FromServices] IValidator<ReactivateRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult<ReactivateRequestResponse>();
        }
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || user.IsActive)
        {
            return Result<ReactivateRequestResponse>.Success(new ReactivateRequestResponse
            {
                Message = "If your account exists and is deactivated, a reactivation email was sent."
            });
        }

        var frontendUrl = configuration["App:FrontendUrl"];

        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "Reactivate",
            expiresAt: DateTime.UtcNow.AddHours(1));

        var link = $"{frontendUrl}/reactivate?token={Uri.EscapeDataString(token)}";
        var body = $"<p>Click the link to reactivate your account:</p><p><a href='{link}'>Reactivate Account</a></p>";

        await emailSender.SendEmailAsync(user.Email, "Reactivate your account", body);

        return Result<ReactivateRequestResponse>.Success(new ReactivateRequestResponse
        {
            Message = "If your account exists and is deactivated, a reactivation email was sent."
        });
    }
}

public class ReactivateRequestResponse
{
    public string Message { get; set; }
}