using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Application.Events;

/// <summary>
/// Factory methods for <see cref="NotificationMessage{TMessage}"/>.
/// </summary>
public static class NotificationMessageFactory
{
    /// <summary>
    /// Wraps a strongly typed message in its notification envelope.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to wrap.</param>
    /// <returns>The notification envelope.</returns>
    public static object Create<TMessage>(TMessage message)
        where TMessage : class
        => new NotificationMessage<TMessage>(message);

    /// <summary>
    /// Wraps a domain event in an envelope typed to its runtime message type, so handlers
    /// registered for the concrete event type receive it. The dedicated overload guarantees
    /// the envelope is not opened on the <see cref="IDomainEvent"/> interface itself.
    /// </summary>
    /// <param name="domainEvent">The domain event to wrap.</param>
    /// <returns>The notification envelope.</returns>
    public static object Create(IDomainEvent domainEvent)
        => CreateByRuntimeType(domainEvent.GetType(), domainEvent);

    /// <summary>
    /// Wraps a message whose runtime type is only known at runtime.
    /// </summary>
    /// <param name="message">The message to wrap.</param>
    /// <returns>The notification envelope.</returns>
    public static object Create(object message)
        => CreateByRuntimeType(message.GetType(), message);

    private static object CreateByRuntimeType(Type messageType, object message)
    {
        Type envelopeType = typeof(NotificationMessage<>).MakeGenericType(messageType);
        return Activator.CreateInstance(envelopeType, message) ?? throw new InvalidOperationException("Failed to create the notification envelope.");
    }
}
