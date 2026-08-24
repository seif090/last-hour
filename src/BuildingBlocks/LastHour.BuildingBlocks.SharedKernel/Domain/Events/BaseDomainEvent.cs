using System.Text.Json.Serialization;

namespace LastHour.BuildingBlocks.SharedKernel.Domain.Events;

/// <summary>
/// Provides a base class for domain events with built-in event identity and occurrence timestamp.
/// </summary>
public abstract class BaseDomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDomainEvent"/> class.
    /// </summary>
    protected BaseDomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDomainEvent"/> class with specific event identity.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="occurredOn">The timestamp when the event occurred.</param>
    [JsonConstructor]
    protected BaseDomainEvent(Guid eventId, DateTime occurredOn)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
    }

    /// <inheritdoc/>
    [JsonInclude]
    public Guid EventId { get; init; }

    /// <inheritdoc/>
    [JsonInclude]
    public DateTime OccurredOn { get; init; }
}
