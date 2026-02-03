using RenuMate.Api.Services.Contracts;

namespace RenuMate.Api.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    public string BuildConfirmEmailMessage(string confirmLink)
    {
        return $@"
        <div style='font-family:Arial, sans-serif; font-size:14px; color:#333;'>
            <p>Hello,</p>

            <p>Thank you for registering. Please confirm your email by clicking the button below:</p>

            <p style='margin:20px 0;'>
                <a href='{confirmLink}'
                   style='background-color:#4CAF50; color:white; padding:10px 18px; text-decoration:none;
                          border-radius:5px; font-weight:bold;'>
                    Confirm Email
                </a>
            </p>

            <p>This confirmation link is valid for <strong>24 hours</strong>.</p>

            <p>If you did not create an account, you can safely ignore this email.</p>

            <p>Best regards,<br/>RenuMate.Api</p>
        </div>";
    }
    
    public string BuildPasswordResetMessage(string userName, string resetLink)
    {
        return $@"
        <div style='font-family:Arial, sans-serif; font-size:14px; color:#333;'>
            <p>Hello {userName},</p>

            <p>You requested to reset your password. Click the button below to choose a new one:</p>

            <p style='margin:20px 0;'>
                <a href='{resetLink}'
                   style='background-color:#4CAF50; color:white; padding:10px 18px; text-decoration:none;
                          border-radius:5px; font-weight:bold;'>
                    Reset Password
                </a>
            </p>

            <p>This link is valid for <strong>30 minutes</strong>.</p>

            <p>If you did not request a password reset, you can safely ignore this email.</p>

            <p>Best regards,<br/>RenuMate.Api</p>
        </div>";
    }
    
    public string BuildEmailChangeConfirmationMessage(string userName, string newEmail, string confirmLink)
    {
        return $@"
        <div style='font-family:Arial, sans-serif; font-size:14px; color:#333;'>
            <p>Hello {userName},</p>

            <p>You requested to change your email address to 
               <strong>{newEmail}</strong>.</p>

            <p>Please confirm this change by clicking the button below:</p>

            <p style='margin:20px 0;'>
                <a href='{confirmLink}'
                   style='background-color:#4CAF50; color:white; padding:10px 18px; text-decoration:none;
                          border-radius:5px; font-weight:bold;'>
                    Confirm Email Change
                </a>
            </p>

            <p>This link is valid for <strong>24 hours</strong>.</p>

            <p>If you did not request this change, you can safely ignore this email.</p>

            <p>Best regards,<br/>RenuMate.Api</p>
        </div>";
    }

    public string BuildUserReactivateMessage(string userName, string confirmLink)
    {
        return $@"
        <div style='font-family:Arial, sans-serif; font-size:14px; color:#333;'>
            <p>Hello {userName},</p>

            <p>We received a request to reactivate your account associated with this email address.</p>
            <p>To reactivate your account, please click the button below:</p>

            <p style='margin:20px 0;'>
                <a href='{confirmLink}'
                   style='background-color:#4CAF50; color:white; padding:10px 18px; text-decoration:none;
                          border-radius:5px; font-weight:bold;'>
                    Reactivate Account
                </a>
            </p>

            <p>This link is valid for <strong>1 hour</strong>.</p>

            <p>If you did not request this change, you can safely ignore this email.</p>

            <p>Best regards,<br/>RenuMate.Api</p>
        </div>";
    }
    
    public string BuildSubscriptionReminderEmail(
        string userName, 
        string subscriptionName, 
        string plan, 
        DateOnly startDate,
        DateOnly renewalDate, 
        decimal cost, 
        string currency,
        string period, 
        string? note)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysRemaining = renewalDate.DayNumber - today.DayNumber;
        
        var countdownText = daysRemaining switch
        {
            0 => "is due <strong>today</strong>!",
            1 => "will renew <strong>tomorrow</strong>.",
            _ => $"will renew in <strong>{daysRemaining} days</strong>."
        };

        return $@"
            <div style='font-family: sans-serif; color: #334155; max-width: 500px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 16px; overflow: hidden;'>
                <div style='background-color: #4f46e5; padding: 20px; text-align: center;'>
                    <h2 style='color: #ffffff; margin: 0;'>Subscription Reminder</h2>
                </div>
                
                <div style='padding: 24px;'>
                    <p style='font-size: 16px;'>Hi {userName},</p>
                    <p style='font-size: 16px;'>
                        Your <strong>{subscriptionName}</strong> subscription {countdownText}
                    </p>
                    
                    <table width='100%' style='background-color: #f8fafc; border-radius: 12px; padding: 16px; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 12px; font-weight: bold; text-transform: uppercase;'>Cost</td>
                            <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #4f46e5;'>{cost} {currency}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 12px; font-weight: bold; text-transform: uppercase;'>Renewal Date</td>
                            <td style='padding: 8px 0; text-align: right; font-weight: bold;'>{renewalDate:dd.MM.yyyy}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 12px; font-weight: bold; text-transform: uppercase;'>Plan</td>
                            <td style='padding: 8px 0; text-align: right; font-weight: bold;'>{plan}</td>
                        </tr>
                    </table>

                    {(!string.IsNullOrEmpty(note) ? $@"
                    <div style='border-left: 4px solid #e2e8f0; padding-left: 12px; font-style: italic; color: #475569; margin: 20px 0;'>
                        ""{note}""
                    </div>" : "")}

                    <p style='font-size: 14px; color: #94a3b8; text-align: center; margin-top: 30px;'>
                        Tracked via RenuMate
                    </p>
                    <p style='font-size: 10px; color: #94a3b8; text-align: center;'>
                        This is an automated message. Please do not reply to this email. 
                    </p>
                </div>
            </div>";
    }
}