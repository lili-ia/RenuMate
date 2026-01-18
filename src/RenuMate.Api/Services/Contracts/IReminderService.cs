namespace RenuMate.Api.Services.Contracts;

public interface IReminderService
{
    Task ProcessDueRemindersAsync(CancellationToken ct = default);
}