using RenuMate.Auth.ConfirmEmail;
using RenuMate.Auth.Login;
using RenuMate.Auth.Register;
using RenuMate.Auth.RequestEmailChange;
using RenuMate.Auth.RequestPasswordReset;
using RenuMate.Auth.ResendEmailConfirmation;
using RenuMate.Auth.ResetPassword;
using RenuMate.Reminders.Create;
using RenuMate.Reminders.Delete;
using RenuMate.Reminders.GetAllForSubscription;
using RenuMate.Reminders.Update;
using RenuMate.Subscriptions.Create;
using RenuMate.Subscriptions.Delete;
using RenuMate.Subscriptions.GetAllForUser;
using RenuMate.Subscriptions.GetDetailsById;
using RenuMate.Subscriptions.SetMuteStatus;
using RenuMate.Subscriptions.Update;
using RenuMate.Users.Deactivate;
using RenuMate.Users.GetInfo;
using RenuMate.Users.Reactivate;
using RenuMate.Users.RequestReactivate;

namespace RenuMate.Extensions;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        ConfirmEmailEndpoint.Map(app);
        LoginUserEndpoint.Map(app);
        RegisterUserEndpoint.Map(app);
        PasswordResetRequestEndpoint.Map(app);
        ResetPasswordEndpoint.Map(app);
        RequestEmailChangeEndpoint.Map(app);
        ResendEmailConfirmationEndpoint.Map(app);
        
        DeactivateUserEndpoint.Map(app);
        ReactivateUserEndpoint.Map(app);
        RequestUserReactivateEndpoint.Map(app);
        GetUserInfoEndpoint.Map(app);

        CreateSubscriptionEndpoint.Map(app);
        UpdateSubscriptionEndpoint.Map(app);
        DeleteSubscriptionEndpoint.Map(app);
        GetSubscriptionDetailsByIdEndpoint.Map(app);
        GetAllSubscriptionsForUserEndpoint.Map(app);
        SetSubscriptionMuteStatusEndpoint.Map(app);
        
        CreateReminderEndpoint.Map(app);
        DeleteReminderEndpoint.Map(app);
        GetAllRemindersForSubscriptionEndpoint.Map(app);
        UpdateReminderEndpoint.Map(app);
    }
}