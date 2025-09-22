namespace RenuMate.Subscriptions.Update;

public class UpdateSubscriptionResponse
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public DateTime RenewalDate { get; set; }

    public string Cost { get; set; } = null!;
    
    public string? Note { get; set; }
    
    public string? CancelLink { get; set; }
    
    public string? PicLink { get; set; }
}