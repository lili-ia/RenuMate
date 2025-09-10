using RenuMate.Enums;

namespace RenuMate.DTOs;

public class SubscriptionDto
{
    public string Name { get; set; } = null!;
    
    public SubscriptionType Type { get; set; }
    
    public int? CustomPeriodInDays { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime RenewalDate { get; set; }
    
    public int DaysLeft => (RenewalDate - DateTime.UtcNow).Days;
    
    public decimal Cost { get; set; }

    public Currency Currency { get; set; }
    
    public string? Note { get; set; }
}