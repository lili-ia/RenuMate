namespace RenuMate.Api.Services.Contracts;

public interface IPendingEmailService
{
    Task ProcessPendingEmailsAsync(CancellationToken ct = default);
}