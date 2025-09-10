using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenuMate.Common;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Reminders.UpdateMuteStatus;

public class UpdateReminderMuteStatusEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) => app
        .MapPatch("api/reminders/{reminderId:guid}", Handle);
    
     private static async Task<IResult> Handle(
        [FromRoute] Guid reminderId,
        [FromBody] UpdateReminderMuteStatusRequest request,
        [FromServices] RenuMateDbContext db,
        [FromServices] IUserContext userContext,
        [FromServices] ILogger<UpdateReminderMuteStatusEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }
        
        try
        {
            var rows = await db.Reminders
                .Where(r => r.Id == reminderId && r.Subscription.UserId == userId)
                .ExecuteUpdateAsync(setter =>
                    setter.SetProperty(r => r.IsMuted, request.IsMuted), cancellationToken);

            if (rows == 0)
            {
                return Results.NotFound("Reminder not found.");
            }
            
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "Error while updating mute status for reminder {ReminderId}.", reminderId);
            
            return Results.InternalServerError("An internal error occurred.");
        }
    }
}