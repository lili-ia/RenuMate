using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Entities;

public class Tag : BaseEntity
{
    public string Name { get; private set; }
    
    public string Color { get; private set; }
    
    public bool IsSystem { get; private set; }
    
    public Guid? UserId { get; private set; }
    
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();
    
    public static Tag CreateSystem(Guid tagId, string name, string color, DateTime createdAt)
    {
        return new Tag(name, color, true, null, tagId, createdAt);
    }

    public static Tag CreateUserTag(string name, string color, Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("User ID cannot be empty for user tag.");
        }
        
        return new Tag(name, color, false, userId, null, null);
    }

    public void Update(string name, string color)
    {
        if (IsSystem)
        {
            throw new DomainConflictException("System tags cannot be modified.");
        }
        
        SetName(name);
        SetColor(color);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Tag name cannot be empty.");
        }
        
        if (name.Length > 50)
        {
            throw new DomainValidationException("Tag name is too long.");
        }
        
        Name = name.Trim();
    }

    private void SetColor(string color)
    {
        if (color.Length > 10)
        {
            throw new DomainValidationException("Invalid tag color format.");
        }
        
        if (string.IsNullOrWhiteSpace(color))
        {
            color = "#808080"; 
        }
        
        Color = color;
    }
    
    private Tag() { }

    private Tag(string name, string color, bool isSystem, Guid? userId, Guid? tagId, DateTime? createdAt)
    {
        Id = tagId ?? Guid.NewGuid();
        CreatedAt = createdAt ?? DateTime.UtcNow;
        IsSystem = isSystem;
        UserId = userId;
        
        SetName(name);
        SetColor(color);
    }
    
    private readonly List<Subscription> _subscriptions = [];
}