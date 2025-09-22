namespace RenuMate.Subscriptions.Create;

public class CreateSubscriptionRequest
{
    public string Name { get; set; } = null!;

    public string Plan { get; set; } = null!;
    
    public int? CustomPeriodInDays { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public decimal Cost { get; set; }

    public string Currency { get; set; } = null!;
    
    public string? Note { get; set; }
    
    public string? CancelLink { get; set; }
    
    public string? PicLink { get; set; }
}