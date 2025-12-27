namespace RenuMate.Entities;

public class User : BaseEntity
{
    public required string Auth0Id { get; set; }
    
    public required string Email { get; set; }
    
    public required string Name{ get; set; }

    public bool EmailConfirmed { get; set; }

    public bool IsActive { get; set; }
    
    public bool IsMetadataSynced { get; set; } = false;

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}