using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Entities;

public class User : BaseEntity
{
    public string Auth0Id { get; private set; }
    
    public string Email { get; private set; }
    
    public string Name { get; private set; }
    
    public bool EmailConfirmed { get; private set; }
    
    public bool IsActive { get; private set; }
    
    public bool IsMetadataSynced { get; private set; }

    public static User Create(string auth0Id, string email, string name)
    {
        if (string.IsNullOrWhiteSpace(auth0Id))
        {
            throw new DomainValidationException("Auth0Id is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException("Email is required.");
        }

        if (!email.Contains("@") || email.Length > 256)
        {
            throw new DomainValidationException("Invalid email format.");
        }
        
        return new User(auth0Id, email.ToLower().Trim(), name);
    }
    
    public void ConfirmEmail()
    {
        if (EmailConfirmed) return;
        EmailConfirmed = true;
    }

    public void UpdateMetadata(string name, bool emailConfirmed)
    {
        Name = name;
        EmailConfirmed = emailConfirmed;
        IsMetadataSynced = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }
        
        IsActive = false;
        
        foreach (var sub in _subscriptions.ToList())
        {
            sub.SetMuteStatus(true);
            sub.ClearAllReminderRules();
        }
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainConflictException("Your account is already active.");
        }

        IsActive = true;
    }
    
    public bool UpdateProfile(string email, string name)
    {
        if (Email == email && Name == name)
        {
            return false;
        }

        Email = email;
        Name = name;
        IsMetadataSynced = false;
        
        return true;
    }

    public void MarkMetadataAsSynced()
    {
        IsMetadataSynced = true;
    }
    
    public virtual IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();

    private User() { }

    private User(string auth0Id, string email, string name)
    {
        Auth0Id = auth0Id;
        Email = email;
        Name = name;
        IsActive = true;
        EmailConfirmed = false;
        IsMetadataSynced = false;
    }
    
    private readonly List<Subscription> _subscriptions = [];
}