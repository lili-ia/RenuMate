using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.RequestPasswordRecover;

public class RequestPasswordRecoverEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPost("api/auth/recover-password-request", Handle)
        .WithSummary("Requests a password recover for user.");

    public static async Task<Result<RequestPasswordRecoverResponse>> Handle(
        PasswordRecoverRequest request,
        RenuMateDbContext db,
        IConfiguration configuration,
        IEmailSender emailSender,
        ITokenService tokenService,
        IValidator<PasswordRecoverRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return validation.ToFailureResult<RequestPasswordRecoverResponse>();
        }
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        
        if (user is null)
        {
            return Result<RequestPasswordRecoverResponse>.Success(new RequestPasswordRecoverResponse
            {
                Message = "If an account exists, a password reset email was sent."
            });
        }
        
        var signingKey = configuration["Jwt:SigningKey"];

        if (signingKey is null)
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }
        
        var token = tokenService.CreateToken(
            userId: user.Id.ToString(),
            email: user.Email,
            purpose: "PasswordRecover",
            expiresAt: DateTime.UtcNow.AddMinutes(30));
        
        var frontendUrl = configuration["App:FrontendUrl"];
        
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("Frontend Url is not configured.");
        }
        
        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var body = $"<p>Click the link below to reset your password:</p>" +
                   $"<p><a href='{resetLink}'>Reset Password</a></p>";
        
        await emailSender.SendEmailAsync(user.Email, "Password Reset", body);
        
        return Result<RequestPasswordRecoverResponse>.Success(new RequestPasswordRecoverResponse
        {
            Message = "If an account exists, a password reset email was sent."
        });
    }
}

public class RequestPasswordRecoverResponse
{
    public string Message { get; set; }
}