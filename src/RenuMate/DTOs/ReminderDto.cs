namespace RenuMate.DTOs;

public class ReminderDto
{
    public Guid Id { get; set; }

    public int DaysBeforeRenewal { get; set; }  
    
    public TimeSpan NotifyTime { get; set; }
    
    public DateTime NextReminder { get; set; }
    
    public bool IsMuted { get; set; } = false;
}