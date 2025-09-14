using Microsoft.EntityFrameworkCore;
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

        var reminders = await _db.Reminders
            .Include(r => r.Subscription)
            .ThenInclude(s => s.User)
            .Where(r => !r.IsMuted)
            .ToListAsync();

        var sentCount = 0;
        
        foreach (var r in reminders)
        {
            var subscription = r.Subscription;
            var email = subscription.User.Email;

            if (r.NextReminder <= now)
            {
                var period = subscription.CustomPeriodInDays.HasValue
                    ? $"{subscription.CustomPeriodInDays.Value} days"
                    : subscription.Type.ToString();

                var note = string.IsNullOrWhiteSpace(subscription.Note)
                    ? "No additional notes"
                    : subscription.Note;

                var subject = $"Reminder: Your subscription \"{subscription.Name}\" is active";

                var body = @$"
                    <p>Hi!
                    This is a friendly reminder about your subscription <strong>{subscription.Name}</strong>.</p>
                    <p>Subscription details:</p>
                    <ul>
                        <li>Type: {subscription.Type}</li>
                        <li>Start Date: {subscription.StartDate:dd.MM.yyyy}</li>
                        <li>Renewal Date: {subscription.RenewalDate:dd.MM.yyyy}</li>
                        <li>Cost: {subscription.Cost} {subscription.Currency}</li>
                        <li>Period: {period}</li>
                        <li>Note: {note}</li>
                    </ul>
                    <p>Thank you for using our service!</p>";

                await _emailService.SendEmailAsync(email, subject, body);
                
                r.NextReminder = subscription.RenewalDate
                    .AddDays(-r.DaysBeforeRenewal)
                    .Add(r.NotifyTime);

                sentCount++;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Sent {RemindersCount} emails.", sentCount);
        }
    }
}