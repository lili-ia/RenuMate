namespace RenuMate.Entities;

public class ReminderOccurrence : BaseEntity
{
    public Guid ReminderRuleId { get; set; }

    public ReminderRule ReminderRule { get; set; } = null!;
    
    public DateTime ScheduledAt { get; set; }
    
    public bool IsSent { get; set; }
    
    public DateTime? SentAt { get; set; }
}