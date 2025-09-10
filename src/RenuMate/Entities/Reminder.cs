namespace RenuMate.Entities;

public class Reminder : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    
    public Subscription Subscription { get; set; } = null!;

    public int DaysBeforeRenewal { get; set; }  
    
    public TimeSpan NotifyTime { get; set; } 
    
    public bool IsMuted { get; set; } = false;
}