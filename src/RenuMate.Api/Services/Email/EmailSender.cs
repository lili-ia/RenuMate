using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RenuMate.Api.Services.Contracts;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;

namespace RenuMate.Api.Services.Email;

public class EmailSender(IOptions<EmailSenderOptions> options) : IEmailSender
{
    private readonly EmailSenderOptions _options = options.Value;

    public async Task<EmailSenderResponse> SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        var client = new SendGridClient(new SendGridClientOptions
        {
            ApiKey = _options.ApiKey,
            HttpErrorAsException = true
        });

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

        try
        {
            await client.SendEmailAsync(msg, ct);
            
            return new EmailSenderResponse(IsSuccess: true, ErrorMessage: null);
        }
        catch (Exception ex)
        {
            var errorResponse = JsonConvert.DeserializeObject<SendGridErrorResponse>(ex.Message);
            
            return new EmailSenderResponse(IsSuccess: false, ErrorMessage: errorResponse?.ToString());
        }
    }
}