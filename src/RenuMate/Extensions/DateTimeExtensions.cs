using RenuMate.Enums;

namespace RenuMate.Extensions;

public static class DateTimeExtensions
{
    public static DateTime AddPeriod(this DateTime date, SubscriptionPlan subscriptionPlan, int? customDays = null)
    {
        return subscriptionPlan switch
        {
            SubscriptionPlan.Custom when customDays.HasValue 
                => date.AddDays(customDays.Value),

            SubscriptionPlan.Monthly 
                => date.AddMonths(1),

            SubscriptionPlan.Quarterly 
                => date.AddMonths(3),

            SubscriptionPlan.Annual 
                => date.AddYears(1),

            _ => throw new ArgumentOutOfRangeException(nameof(subscriptionPlan), subscriptionPlan, null)
        };
    }
}