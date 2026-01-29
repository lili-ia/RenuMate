namespace RenuMate.Api.Services.Contracts;

public interface IEmailTemplateService
{
    string BuildUserReactivateMessage(string userName, string confirmLink);

    string BuildSubscriptionReminderEmail(
        string userName,
        string subscriptionName,
        string plan,
        DateOnly startDate,
        DateOnly renewalDate,
        decimal cost,
        string currency,
        string period,
        string? note);
}