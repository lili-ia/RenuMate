using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Auth.ResetPassword;

public class ResetPasswordEndpoint : Endpoint<ResetPasswordRequest, Result<ResetPasswordResponse>>
{
    private readonly RenuMateDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    
    public ResetPasswordEndpoint(RenuMateDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/auth/reset-password");
    }

    public override async Task<Result<ResetPasswordResponse>> HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var signingKeyConfig = _configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKeyConfig))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }
        
        var signingKey = Encoding.UTF8.GetBytes(signingKeyConfig);

        try
        {
            tokenHandler.ValidateToken(req.Token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var stringUserId = jwtToken.Claims.First(c => c.Type == "UserId").Value;
            var purpose = jwtToken.Claims.First(c => c.Type == "Purpose").Value;

            if (purpose != "PasswordReset")
            {
                return Result<ResetPasswordResponse>.Failure("Invalid token purpose.", ErrorType.BadRequest);
            }

            if (!Guid.TryParse(stringUserId, out var userId))
            {
                return Result<ResetPasswordResponse>.Failure("Invalid token.", ErrorType.BadRequest);
            }
            
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return Result<ResetPasswordResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
            }

            var newHashedPassword = await _passwordHasher.HashPassword(req.NewPassword);
            user.PasswordHash = newHashedPassword;
            await _db.SaveChangesAsync(ct);
            
            var accessToken = JwtBearer.CreateToken(o =>
            {
                o.SigningKey = signingKeyConfig;
                o.ExpireAt = DateTime.UtcNow.AddHours(24);
            
                o.User.Claims.Add(("sub", user.Id.ToString()));
                o.User.Claims.Add(("email", user.Email));
                o.User.Claims.Add(("jti", Guid.NewGuid().ToString())); 
                o.User.Claims.Add(("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
            });
            
            return Result<ResetPasswordResponse>.Success(new ResetPasswordResponse
            {
                Token = accessToken
            });
        }
        catch (NpgsqlException npgsqlException)
        {
            return Result<ResetPasswordResponse>.Failure("An internal error occurred.", ErrorType.ServerError);
        }
        catch (Exception ex)
        {
            return Result<ResetPasswordResponse>.Failure("Invalid or expired token.");
        }
    }
}