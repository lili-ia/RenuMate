using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.RequestPasswordRecover;

public class RequestPasswordRecoverEndpoint : Endpoint<PasswordRecoverRequest, Result<RequestPasswordRecoverResponse>>
{
    private readonly RenuMateDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    
    public RequestPasswordRecoverEndpoint(RenuMateDbContext db, IConfiguration configuration, IEmailSender emailSender)
    {
        _db = db;
        _configuration = configuration;
        _emailSender = emailSender;
    }

    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/auth/recover-password-request");
    }

    public override async Task<Result<RequestPasswordRecoverResponse>> HandleAsync(
        PasswordRecoverRequest req, 
        CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
        
        if (user is null)
        {
            return Result<RequestPasswordRecoverResponse>.Success(new RequestPasswordRecoverResponse
            {
                Message = "If an account exists, a password reset email was sent."
            });
        }
        
        var signingKey = _configuration["Jwt:SigningKey"];

        if (signingKey is null)
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }
        
        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = signingKey;
            o.ExpireAt = DateTime.UtcNow.AddHours(1);
            o.User["UserId"] = user.Id.ToString();
            o.User["Purpose"] = "PasswordReset";
        });
        
        var frontendUrl = _configuration["App:FrontendUrl"];
        
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("Frontend Url is not configured.");
        }
        
        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var body = $"<p>Click the link below to reset your password:</p>" +
                   $"<p><a href='{resetLink}'>Reset Password</a></p>";
        
        await _emailSender.SendEmailAsync(user.Email, "Password Reset", body);
        
        return Result<RequestPasswordRecoverResponse>.Success(new RequestPasswordRecoverResponse
        {
            Message = "If an account exists, a password reset email was sent."
        });
    }
}