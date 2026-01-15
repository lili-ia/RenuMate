using Microsoft.EntityFrameworkCore;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class ReminderService(
    RenuMateDbContext db,
    IEmailSender emailService,
    ILogger<ReminderService> logger,
    IEmailTemplateService emailTemplateService)
    : IReminderService
{
    public async Task ProcessDueRemindersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var occurrences = await db.ReminderOccurrences
            .Include(o => o.ReminderRule)
                .ThenInclude(r => r.Subscription)
                .ThenInclude(s => s.User)
            .Where(o => !o.ReminderRule.Subscription.IsMuted 
                        && !o.IsSent && o.ScheduledAt <= now 
                        && o.ReminderRuleId.HasValue)
            .ToListAsync(ct);

        if (occurrences.Count == 0)
        {
            return;
        }
        
        foreach (var occurrence in occurrences)
        {
            var rule = occurrence.ReminderRule;
            var sub = rule.Subscription;

            try
            {
                var period = sub.CustomPeriodInDays.HasValue
                    ? $"{sub.CustomPeriodInDays.Value} days"
                    : sub.Plan.ToString();

                var note = string.IsNullOrWhiteSpace(sub.Note)
                    ? "No additional notes"
                    : sub.Note;

                var subject = $"Reminder: Your subscription \"{sub.Name}\" is active";

                var body = emailTemplateService.BuildSubscriptionReminderEmail(
                    userName: sub.User.Name,
                    subscriptionName: sub.Name,
                    plan: sub.Plan.ToString(),
                    startDate: sub.StartDate,
                    renewalDate: sub.RenewalDate,
                    cost: sub.Cost,
                    currency: sub.Currency.ToString(),
                    period: period,
                    note: note);

                var sent = await emailService.SendEmailAsync(sub.User.Email, subject, body, ct);

                if (sent)
                {
                    occurrence.MarkAsSent();

                    var nextOccurrence = rule.CreateOccurrence(sub.RenewalDate);

                    if (nextOccurrence is not null)
                    {
                        rule.AddOccurrence(nextOccurrence);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process reminder {Id}", occurrence.Id);
            }
        }
        
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Sent {RemindersCount} emails.", occurrences.Count);
    }
}