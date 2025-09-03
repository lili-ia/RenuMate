using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Entities;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.Register;

public class RegisterUserEndpoint : Endpoint<RegisterUserRequest, Result<RegisterUserResponse>>
{
    private readonly RenuMateDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    
    public RegisterUserEndpoint(
        RenuMateDbContext db, 
        IPasswordHasher passwordHasher, 
        IEmailSender emailSender, 
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/auth/register");
    }

    public override async Task<Result<RegisterUserResponse>> HandleAsync(RegisterUserRequest req, CancellationToken ct)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Email == req.Email, ct);

        if (userExists)
        {
            return Result<RegisterUserResponse>.Failure("User with this email already registered.", 
                ErrorType.BadRequest);
        }

        var hashedPassword = await _passwordHasher.HashPassword(req.Password);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Email = req.Email,
            PasswordHash = hashedPassword,
            IsEmailConfirmed = false,
            IsActive = true
        };

        try
        {
            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return Result<RegisterUserResponse>.Failure("Internal error occurred.", ErrorType.ServerError);
        }
        
        var signingKey = _configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = signingKey;
            o.ExpireAt = DateTime.UtcNow.AddHours(24);
            o.User["UserId"] = user.Id.ToString();
            o.User["Purpose"] = "EmailConfirmation";
        });
        
        var frontendUrl = _configuration["App:FrontendUrl"];

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("Frontend Url is not configured.");
        }
        
        var confirmLink = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
        
        var body = $"<p>Please confirm your email by clicking the link below:</p>" +
                   $"<p><a href='{confirmLink}'>Confirm Email</a></p>";

        await _emailSender.SendEmailAsync(req.Email, "Confirm your email", body);
        
        return Result<RegisterUserResponse>.Success(new RegisterUserResponse
        {
            Message = "Account created successfully. Please check your email to verify your account."
        });
    }
}