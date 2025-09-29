namespace RenuMate.DTOs;

public class SubscriptionDetailsDto : SubscriptionDto
{
    public List<ReminderDto> Reminders { get; set; } = [];
}