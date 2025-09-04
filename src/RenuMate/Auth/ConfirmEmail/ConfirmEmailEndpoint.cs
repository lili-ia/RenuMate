using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RenuMate.Common;
using RenuMate.Persistence;

namespace RenuMate.Auth.ConfirmEmail;

public class ConfirmEmailEndpoint : EndpointWithoutRequest<Result<ConfirmEmailResponse>>
{
    private readonly IConfiguration _configuration;
    private readonly RenuMateDbContext _db;
    
    public ConfirmEmailEndpoint(IConfiguration configuration, RenuMateDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    public override void Configure()
    {
        AllowAnonymous();
        Get("/api/auth/confirm-email");
    }

    public override async Task<Result<ConfirmEmailResponse>> HandleAsync(CancellationToken ct)
    {
        var token = Query<string>("token");

        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<ConfirmEmailResponse>.Failure("Token is missing.", ErrorType.BadRequest);
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        
        var signingKeyConfig = _configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKeyConfig))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }
        
        var signingKey = Encoding.UTF8.GetBytes(signingKeyConfig);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
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

            if (purpose != "EmailConfirmation")
            {
                return Result<ConfirmEmailResponse>.Failure("Invalid token purpose.", ErrorType.BadRequest);
            }

            if (!Guid.TryParse(stringUserId, out var userId))
            {
                return Result<ConfirmEmailResponse>.Failure("Invalid token.", ErrorType.BadRequest);
            }
            
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return Result<ConfirmEmailResponse>.Failure("Invalid or expired token.", ErrorType.BadRequest);
            }

            if (user.IsEmailConfirmed)
            {
                return Result<ConfirmEmailResponse>.Failure("You already confirmed your email.", 
                    ErrorType.Conflict);
            }

            user.IsEmailConfirmed = true;
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
            
            return Result<ConfirmEmailResponse>.Success(new ConfirmEmailResponse
            {
                Token = accessToken
            });
        }
        catch (NpgsqlException npgsqlException)
        {
            return Result<ConfirmEmailResponse>.Failure("An internal error occurred.", ErrorType.ServerError);
        }
        catch (Exception ex)
        {
            return Result<ConfirmEmailResponse>.Failure("Invalid or expired token.");
        }
    }
}