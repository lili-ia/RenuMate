using RenuMate.Api.Reminders.Create;
using RenuMate.Api.Reminders.Delete;
using RenuMate.Api.Reminders.GetAllForSubscription;
using RenuMate.Api.Subscriptions.Create;
using RenuMate.Api.Subscriptions.Delete;
using RenuMate.Api.Subscriptions.GetAllForUser;
using RenuMate.Api.Subscriptions.GetDetailsById;
using RenuMate.Api.Subscriptions.GetSummary;
using RenuMate.Api.Subscriptions.SetMuteStatus;
using RenuMate.Api.Subscriptions.Update;
using RenuMate.Api.Users.Deactivate;
using RenuMate.Api.Users.GetInfo;
using RenuMate.Api.Users.Reactivate;
using RenuMate.Api.Users.RequestReactivate;
using RenuMate.Api.Users.Sync;

namespace RenuMate.Api.Extensions;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        DeactivateUserEndpoint.Map(app);
        ReactivateUserEndpoint.Map(app);
        RequestUserReactivateEndpoint.Map(app);
        GetUserInfoEndpoint.Map(app);
        SyncUserEndpoint.Map(app);
        
        CreateSubscriptionEndpoint.Map(app);
        UpdateSubscriptionEndpoint.Map(app);
        DeleteSubscriptionEndpoint.Map(app);
        GetSubscriptionDetailsByIdEndpoint.Map(app);
        GetAllSubscriptionsForUserEndpoint.Map(app);
        SetSubscriptionMuteStatusEndpoint.Map(app);
        GetSubscriptionsSummaryEndpoint.Map(app);  
        
        CreateReminderEndpoint.Map(app);
        DeleteReminderEndpoint.Map(app);
        GetAllRemindersForSubscriptionEndpoint.Map(app);
    }
}