using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Services;

public class PendingEmailService(RenuMateDbContext db, IEmailSender emailSender, ILogger<PendingEmailService> logger) 
    : IPendingEmailService
{
    public async Task ProcessPendingEmailsAsync(CancellationToken ct = default)
    {
        var emails = await db.PendingEmails
            .Where(e => !e.IsSent && e.RetryCount < e.MaxRetries)
            .OrderBy(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var email in emails)
        {
            var emailSenderResponse = await emailSender.SendEmailAsync(
                email.To, email.Subject, email.Body, CancellationToken.None);

            var now = DateTime.UtcNow;
            
            if (emailSenderResponse.IsSuccess)
            {
                email.MarkSent(now);
            }
            else
            {
                var message = emailSenderResponse.ErrorMessage;
                email.RegisterFailure(message, now);
                logger.LogWarning(message, "Retry failed for email {EmailId}", email.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}