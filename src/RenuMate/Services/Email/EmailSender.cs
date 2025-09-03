using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RenuMate.Services.Contracts;

namespace RenuMate.Services.Email;

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    
    public EmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.User, _options.Password)
        };

        var mail = new MailMessage(_options.FromEmail, to, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mail);
    }
}