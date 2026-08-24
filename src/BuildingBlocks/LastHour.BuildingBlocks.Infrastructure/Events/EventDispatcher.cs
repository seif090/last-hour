using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using MediatR;

namespace LastHour.BuildingBlocks.Infrastructure.Events;

/// <summary>
/// In-process <see cref="IEventDispatcher"/> that forwards domain events to their registered
/// handlers through the MediatR pipeline. Each event is wrapped in a
/// <see cref="NotificationMessage{TMessage}"/> envelope; events are dispatched sequentially in
/// the order they were raised so handlers observe a deterministic sequence.
/// </summary>
public sealed class EventDispatcher : IEventDispatcher
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventDispatcher"/> class.
    /// </summary>
    /// <param name="mediator">The mediator used to publish event notifications.</param>
    public EventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        object notification = NotificationMessageFactory.Create(domainEvent);
        await _mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
