namespace RenuMate.Reminders.Create;

public class CreateReminderResponse
{
    public Guid Id { get; set; }
    
    public Guid SubscriptionId { get; set; }

    public int DaysBeforeRenewal { get; set; }  
    
    public TimeSpan NotifyTime { get; set; } 
    
    public DateTime NextReminder { get; set; } 
}