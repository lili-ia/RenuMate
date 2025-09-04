using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.RequestReactivate;

public class RequestUserReactivateEndpoint : Endpoint<ReactivateRequest, Result<ReactivateRequestResponse>>
{
    private readonly RenuMateDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public RequestUserReactivateEndpoint(RenuMateDbContext db, IEmailSender emailSender, IConfiguration configuration)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public override void Configure()
    {
        AllowAnonymous();
        Post("api/users/reactivate-request");
    }

    public override async Task<Result<ReactivateRequestResponse>> HandleAsync(ReactivateRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);

        if (user is null || user.IsActive)
        {
            return Result<ReactivateRequestResponse>.Success(new ReactivateRequestResponse
            {
                Message = "If your account exists and is deactivated, a reactivation email was sent."
            });
        }

        var signingKey = _configuration["Jwt:SigningKey"];
        var frontendUrl = _configuration["App:FrontendUrl"];

        if (string.IsNullOrWhiteSpace(signingKey) || string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("JWT signing key or frontend url is not configured.");
        }
        
        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = signingKey;
            o.ExpireAt = DateTime.UtcNow.AddHours(1);
            o.User["UserId"] = user.Id.ToString();
            o.User["Purpose"] = "Reactivate";
        });

        var link = $"{frontendUrl}/reactivate-account?token={Uri.EscapeDataString(token)}";
        var body = $"<p>Click the link to reactivate your account:</p><p><a href='{link}'>Reactivate Account</a></p>";

        await _emailSender.SendEmailAsync(user.Email, "Reactivate your account", body);

        return Result<ReactivateRequestResponse>.Success(new ReactivateRequestResponse
        {
            Message = "If your account exists and is deactivated, a reactivation email was sent."
        });
    }
}