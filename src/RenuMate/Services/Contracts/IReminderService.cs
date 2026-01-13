namespace RenuMate.Services.Contracts;

public interface IReminderService
{
    Task ProcessDueRemindersAsync(CancellationToken ct = default);
}