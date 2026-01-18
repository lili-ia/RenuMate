using System.ComponentModel.DataAnnotations.Schema;
using MediatR;

namespace RenuMate.Api.Entities;

public class BaseEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    [NotMapped]
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();
    
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    private readonly List<INotification> _domainEvents = [];
    
    protected void AddDomainEvent(INotification eventItem) => _domainEvents.Add(eventItem);
}