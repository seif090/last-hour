using MediatR;

namespace LastHour.BuildingBlocks.Application.Events;

/// <summary>
/// MediatR notification envelope used to deliver framework-agnostic messages to handlers.
/// Domain events implement <see cref="SharedKernel.Domain.Events.IDomainEvent"/>, which does
/// not extend <see cref="INotification"/>, so the dispatcher wraps each event (or arbitrary
/// message) in this envelope before publishing it through MediatR. Handlers register for
/// <see cref="NotificationMessage{TMessage}"/> of their message type.
/// </summary>
/// <typeparam name="TMessage">The type of the message being delivered.</typeparam>
public sealed class NotificationMessage<TMessage> : INotification
    where TMessage : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationMessage{TMessage}"/> class.
    /// </summary>
    /// <param name="message">The message being delivered to handlers.</param>
    public NotificationMessage(TMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the message being delivered to handlers.
    /// </summary>
    public TMessage Message { get; }
}
