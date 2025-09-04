using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RenuMate.Common;
using RenuMate.Persistence;

namespace RenuMate.Users.Reactivate;

public class ReactivateUserEndpoint : EndpointWithoutRequest<Result<ReactivateUserResponse>>
{
    private readonly IConfiguration _configuration;
    private readonly RenuMateDbContext _db;
    
    public ReactivateUserEndpoint(IConfiguration configuration, RenuMateDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    public override void Configure()
    {
        AllowAnonymous();
        Post("api/users/reactivate");
    }

    public override async Task<Result<ReactivateUserResponse>> HandleAsync(CancellationToken ct)
    {
        var token = Query<string>("token");

        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<ReactivateUserResponse>.Failure("Token is missing.", ErrorType.BadRequest);
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

            if (purpose != "Reactivate")
            {
                return Result<ReactivateUserResponse>.Failure("Invalid token purpose.", ErrorType.BadRequest);
            }

            if (!Guid.TryParse(stringUserId, out var userId))
            {
                return Result<ReactivateUserResponse>.Failure("Invalid token.", ErrorType.BadRequest);
            }
            
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return Result<ReactivateUserResponse>.Failure("Invalid or expired token.", 
                    ErrorType.BadRequest);
            }

            if (user.IsActive)
            {
                return Result<ReactivateUserResponse>.Failure("Your account is already active.", 
                    ErrorType.Conflict);
            }

            user.IsActive = true;
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
            
            return Result<ReactivateUserResponse>.Success(new ReactivateUserResponse
            {
                Token = accessToken
            });
        }
        catch (NpgsqlException npgsqlException)
        {
            return Result<ReactivateUserResponse>.Failure("An internal error occurred.", ErrorType.ServerError);
        }
        catch (Exception ex)
        {
            return Result<ReactivateUserResponse>.Failure("Invalid or expired token.");
        }
    }
}