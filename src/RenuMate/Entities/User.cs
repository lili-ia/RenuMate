namespace RenuMate.Entities;

public class User : BaseEntity
{
    public string Email { get; set; }
    
    public string Name { get; set; }

    public string PasswordHash { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; }
}