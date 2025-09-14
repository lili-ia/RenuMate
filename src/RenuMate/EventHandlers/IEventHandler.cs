using RenuMate.Events;

namespace RenuMate.EventHandlers;

public interface IEventHandler
{
    Task HandleAsync(IEvent @event, CancellationToken cancellationToken = default);
}