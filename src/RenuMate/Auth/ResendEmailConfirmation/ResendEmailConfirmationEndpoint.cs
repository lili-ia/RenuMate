using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Auth.Register;
using RenuMate.Common;
using RenuMate.Extensions;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ResendEmailConfirmation;

public class ResendEmailConfirmationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/resend-email-confirmation", Handle)
        .WithSummary("Resends email confirmation.");

    private static async Task<IResult> Handle(
        [FromBody] ResendEmailConfirmationRequest request,
        RenuMateDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ITokenService tokenService,
        IEmailSender emailSender,
        IValidator<ResendEmailConfirmationRequest> validator,
        ILogger<RegisterUserEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult<ResendEmailConfirmationResponse>().ToIResult();
        }
        
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
        {
            return Result<ResendEmailConfirmationResponse>.Failure(
                "User not found.", ErrorType.NotFound).ToIResult();
        }
        
        if (user.IsEmailConfirmed)
        {
            return Result<ResendEmailConfirmationResponse>.Failure(
                "Users with this email already confirmed email.", ErrorType.BadRequest).ToIResult();
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "ConfirmEmail",
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
        
        return Result<ResendEmailConfirmationResponse>.Success(new ResendEmailConfirmationResponse
        {
            Message = "Account created successfully. Please check your email to verify your account."
        }).ToIResult();
    }
}

public class ResendEmailConfirmationResponse
{
    public string Message { get; set; } = null!;
}