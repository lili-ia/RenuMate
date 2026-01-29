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
        return $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.5; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 10px; background: #fafafa; }}
                h1 {{ color: #0056b3; }}
                ul {{ padding-left: 20px; }}
                li {{ margin-bottom: 5px; }}
                .footer {{ font-size: 12px; color: #888; margin-top: 20px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>Hi {userName}!</h1>
                <p>This is a friendly reminder about your subscription <strong>{subscriptionName}</strong>.</p>
                <p>Subscription details:</p>
                <ul>
                    <li>Plan: {plan}</li>
                    <li>Start Date: {startDate:dd.MM.yyyy}</li>
                    <li>Renewal Date: {renewalDate:dd.MM.yyyy}</li>
                    <li>Cost: {cost}{currency}</li>
                    <li>Period: {period}</li>
                    <li>Note: {note}</li>
                </ul>
                <p>Thank you for using our service!</p>
                <div class='footer'>
                    This is an automated message. Please do not reply to this email.
                </div>
            </div>
        </body>
        </html>";
    }
}