using System.ComponentModel.DataAnnotations.Schema;
using MediatR;

namespace RenuMate.Entities;

public class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    private readonly List<INotification> _domainEvents = [];
    [NotMapped]
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(INotification eventItem) => _domainEvents.Add(eventItem);

    public void ClearDomainEvents() => _domainEvents.Clear();
}