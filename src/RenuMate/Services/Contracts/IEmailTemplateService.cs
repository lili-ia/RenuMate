namespace RenuMate.Services.Contracts;

public interface IEmailTemplateService
{
    string BuildConfirmEmailMessage(string confirmLink);
    
    string BuildPasswordResetMessage(string userName, string resetLink);

    string BuildEmailChangeConfirmationMessage(string userName, string newEmail, string confirmLink);

    string BuildUserReactivateMessage(string userName, string confirmLink);

    public string BuildSubscriptionReminderEmail(
        string userName,
        string subscriptionName,
        string plan,
        DateTime startDate,
        DateTime renewalDate,
        decimal cost,
        string currency,
        string period,
        string? note);
}