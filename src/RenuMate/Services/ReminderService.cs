using Microsoft.EntityFrameworkCore;
using RenuMate.Entities;
using RenuMate.Persistence;
using RenuMate.Services.Contracts;

namespace RenuMate.Services;

public class ReminderService : IReminderService
{
    private readonly RenuMateDbContext _db;
    private readonly IEmailSender _emailService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(RenuMateDbContext db, IEmailSender emailService, ILogger<ReminderService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessDueRemindersAsync()
    {
        var now = DateTime.UtcNow;

        var occurrences = await _db.ReminderOccurrences
            .Include(o => o.ReminderRule)
                .ThenInclude(r => r.Subscription)
                .ThenInclude(s => s.User)
            .Where(o => !o.ReminderRule.Subscription.IsMuted && !o.IsSent && o.ScheduledAt <= now)
            .ToListAsync();
        
        foreach (var o in occurrences)
        {
            var subscription = o.ReminderRule.Subscription;
            var email = subscription.User.Email;

            var period = subscription.CustomPeriodInDays.HasValue
                ? $"{subscription.CustomPeriodInDays.Value} days"
                : subscription.Plan.ToString();

            var note = string.IsNullOrWhiteSpace(subscription.Note)
                ? "No additional notes"
                : subscription.Note;

            var subject = $"Reminder: Your subscription \"{subscription.Name}\" is active";

            var body = @$"
                    <p>Hi, {subscription.User.Name}!
                    This is a friendly reminder about your subscription <strong>{subscription.Name}</strong>.</p>
                    <p>Subscription details:</p>
                    <ul>
                        <li>Plan: {subscription.Plan}</li>
                        <li>Start Date: {subscription.StartDate:dd.MM.yyyy}</li>
                        <li>Renewal Date: {subscription.RenewalDate:dd.MM.yyyy}</li>
                        <li>Cost: {subscription.Cost} {subscription.Currency}</li>
                        <li>Period: {period}</li>
                        <li>Note: {note}</li>
                    </ul>
                    <p>Thank you for using our service!</p>";

            var sentSuccess = await _emailService.SendEmailAsync(email, subject, body);

            if (sentSuccess)
            {
                o.IsSent = true;
                o.SentAt = now;
            }
            
            var nextReminderAt = subscription.RenewalDate
                .AddDays(-o.ReminderRule.DaysBeforeRenewal)
                .Add(o.ReminderRule.NotifyTimeUtc);

            if (nextReminderAt <= now)
            {
                continue;
            }
            
            var nextReminder = new ReminderOccurrence
            {
                ReminderRuleId = o.ReminderRule.Id,
                ScheduledAt = nextReminderAt,
                IsSent = false
            };

            _db.ReminderOccurrences.Add(nextReminder);
        }
        
        await _db.SaveChangesAsync();
        _logger.LogInformation("Sent {RemindersCount} emails.", occurrences.Count);
    }
}