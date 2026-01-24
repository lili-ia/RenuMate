using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RenuMate.Api.Tests.Integration;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) 
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();

        if (Context.Request.Headers.TryGetValue("X-Test-Sub", out var sub))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
        }

        if (Context.Request.Headers.TryGetValue("X-Test-Email", out var email))
        {
            claims.Add(new Claim("http://renumate.online/email", email));
        }
        
        if (Context.Request.Headers.TryGetValue("X-Test-UserId", out var userId))
        {
            claims.Add(new Claim("http://renumate.online/user_id", userId));
        }

        if (Context.Request.Headers.TryGetValue("X-Test-Name", out var name))
        {
            claims.Add(new Claim("http://renumate.online/name", name));
        }
        
        if (Context.Request.Headers.TryGetValue("X-Test-EmailVerified", out var emailVerified))
        {
            claims.Add(new Claim("http://renumate.online/email_verified", emailVerified.ToString()));
        }
        
        if (Context.Request.Headers.TryGetValue("X-Test-IsActive", out var isActive))
        {
            claims.Add(new Claim("http://renumate.online/is_active", isActive.ToString()));
        }

        if (claims.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("No test auth headers"));
        }
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}