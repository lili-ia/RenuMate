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
    private readonly IEmailTemplateService _emailTemplateService;

    public ReminderService(
        RenuMateDbContext db, 
        IEmailSender emailService, 
        ILogger<ReminderService> logger, 
        IEmailTemplateService emailTemplateService)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _emailTemplateService = emailTemplateService;
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

            var body = _emailTemplateService.BuildSubscriptionReminderEmail(
                userName: subscription.User.Name,
                subscriptionName: subscription.Name,
                plan: subscription.Plan.ToString(),
                startDate: subscription.StartDate,
                renewalDate: subscription.RenewalDate,
                cost: subscription.Cost,
                currency: subscription.Currency.ToString(),
                period: period,
                note: note);
                
            var sent = await _emailService.SendEmailAsync(email, subject, body);

            if (sent)
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