using RenuMate.Api.Services.Email;

namespace RenuMate.Api.Services.Contracts;

public interface IEmailSender
{
    Task<EmailSenderResponse> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
}