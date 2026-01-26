using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Services.Email;

public class EmailSender(IOptions<EmailSenderOptions> options, ILogger<EmailSender> logger) 
    : IEmailSender
{
    private readonly EmailSenderOptions _options = options.Value;

    public async Task<EmailSenderResponse> SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        
        message.From.Add(new MailboxAddress(_options.FromUser, _options.FromEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, false, ct);
            await client.AuthenticateAsync(_options.UserName, _options.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            
            return new EmailSenderResponse(IsSuccess: true, ErrorMessage: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP error occurred while sending email to {To}", to);
            return new EmailSenderResponse(IsSuccess: false, ErrorMessage: ex.Message);
        }
    }
}