using RenuMate.Enums;

namespace RenuMate.Extensions;

public static class DateTimeExtensions
{
    public static DateTime AddPeriod(this DateTime date, SubscriptionType subscriptionType, int? customDays = null)
    {
        return subscriptionType switch
        {
            SubscriptionType.Custom when customDays.HasValue 
                => date.AddDays(customDays.Value),

            SubscriptionType.Monthly 
                => date.AddMonths(1),

            SubscriptionType.Quarterly 
                => date.AddMonths(3),

            SubscriptionType.Annual 
                => date.AddYears(1),

            _ => throw new ArgumentOutOfRangeException(nameof(subscriptionType), subscriptionType, null)
        };
    }
}