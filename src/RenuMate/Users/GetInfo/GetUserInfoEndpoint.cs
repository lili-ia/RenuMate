using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Users.GetInfo;

public class GetUserInfoEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapGet("api/users/me", Handle)
        .WithSummary("Gets info about current user.")
        .RequireAuthorization();

    private static async Task<IResult> Handle(
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var info = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserInfoResponse
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                MemberSince = u.CreatedAt,
                SubscriptionCount = u.Subscriptions.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null)
        {
            return Results.NotFound(new { Error = "User not found."});
        }
        
        return Results.Ok(info);
    }
}

public class UserInfoResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    
    public DateTime MemberSince { get; set; }
    
    public int SubscriptionCount { get; set; }
}