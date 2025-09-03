using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.Login;

public class LoginUserEndpoint : Endpoint<LoginUserRequest, Result<LoginUserResponse>>
{
    private readonly RenuMateDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    
    public LoginUserEndpoint(
        RenuMateDbContext db, 
        IPasswordHasher passwordHasher, 
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }
    
    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/auth/login");
    }

    public override async Task<Result<LoginUserResponse>> HandleAsync(LoginUserRequest req, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == req.Email, ct);

        if (user is null)
        {
            return Result<LoginUserResponse>.Failure("Invalid email or password.", ErrorType.Unauthorized);
        }
        
        var passwordValid = await _passwordHasher
            .VerifyHashedPassword(req.Password, user.PasswordHash);
        
        if (!passwordValid)
        {
            return Result<LoginUserResponse>.Failure("Invalid email or password.", ErrorType.Unauthorized);
        }

        var signingKey = _configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var accessToken = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = "A secret token signing key";
            o.ExpireAt = DateTime.UtcNow.AddHours(24);
            
            o.User.Claims.Add(("sub", user.Id.ToString()));
            o.User.Claims.Add(("email", user.Email));
            o.User.Claims.Add(("jti", Guid.NewGuid().ToString())); 
            o.User.Claims.Add(("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
        });
        
        return Result<LoginUserResponse>.Success(new LoginUserResponse
        {
            JwtToken = accessToken
        });
    }
}