namespace RenuMate.Entities;

public class Reminder : BaseEntity
{
    public int SubscriptionId { get; set; }
    
    public Subscription Subscription { get; set; } = null!;

    public DateTime ReminderDate { get; set; }
    
    public bool IsSent { get; set; } = false;
    
    public bool IsMuted { get; set; } = false;
}