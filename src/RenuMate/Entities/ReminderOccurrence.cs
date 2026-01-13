namespace RenuMate.Entities;

public class ReminderOccurrence : BaseEntity
{
    public Guid? ReminderRuleId { get; init; }

    public ReminderRule ReminderRule { get; init; } = null!;
    
    public DateTime ScheduledAt { get; set; }
    
    public bool IsSent { get; set; }
    
    public DateTime? SentAt { get; set; }
}