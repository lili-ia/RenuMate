using Microsoft.Extensions.Options;
using RenuMate.Api.Services.Contracts;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RenuMate.Api.Services.Email;

public class EmailSender(IOptions<EmailSenderOptions> options) : IEmailSender
{
    private readonly EmailSenderOptions _options = options.Value;

    public async Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        var client = new SendGridClient(_options.ApiKey);

        var from = new EmailAddress(_options.FromEmail, _options.FromUser);

        var receiver = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, receiver, subject,"", body);

        var trackingSettings = new TrackingSettings
        {
            ClickTracking = new ClickTracking
            {
                Enable = false,
                EnableText = false
            }
        };
        
        msg.TrackingSettings = trackingSettings;
        
        var response = await client.SendEmailAsync(msg, ct);

        return response.IsSuccessStatusCode;
    }
}