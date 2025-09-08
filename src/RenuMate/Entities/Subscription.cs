using RenuMate.Enums;

namespace RenuMate.Entities;

public class Subscription : BaseEntity
{
    public string Name { get; set; } = null!;
    
    public string Type { get; set; } = null!;
    
    public DateTime StartDate { get; set; }
    
    public DateTime RenewalDate { get; set; }
    
    public decimal Cost { get; set; }

    public Currency Currency { get; set; }
    
    public string? Note { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
    
    public ICollection<Reminder> Reminders { get; set; }
}