using RenuMate.Exceptions;

namespace RenuMate.Entities;

public class ReminderOccurrence : BaseEntity
{
    public Guid? ReminderRuleId { get; private set; }
    
    public ReminderRule ReminderRule { get; private set; } = null!;
    
    public DateTime ScheduledAt { get; private set; }
    
    public bool IsSent { get; private set; }
    
    public DateTime? SentAt { get; private set; }

    public static ReminderOccurrence Create(Guid reminderRuleId, DateTime scheduledAt)
    {
        if (scheduledAt <= DateTime.UtcNow)
        {
            throw new DomainValidationException("Cannot schedule a reminder in the past.");
        }
        
        return new ReminderOccurrence(reminderRuleId, scheduledAt);
    }

    public void MarkAsSent()
    {
        if (IsSent)
        {
            throw new DomainConflictException("Reminder has already been sent.");
        }
        
        IsSent = true;
        SentAt = DateTime.UtcNow;
    }

    private ReminderOccurrence() { }

    private ReminderOccurrence(Guid reminderRuleId, DateTime scheduledAt)
    {
        ReminderRuleId = reminderRuleId;
        ScheduledAt = scheduledAt;
        IsSent = false;
    }
}