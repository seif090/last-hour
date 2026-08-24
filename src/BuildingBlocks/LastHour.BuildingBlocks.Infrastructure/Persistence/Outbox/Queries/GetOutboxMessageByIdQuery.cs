using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Retrieves detailed operational diagnostics for a single outbox message by its identifier.
/// </summary>
public sealed record GetOutboxMessageByIdQuery : IQuery<OutboxMessageDetailDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetOutboxMessageByIdQuery"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the target outbox message.</param>
    public GetOutboxMessageByIdQuery(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the unique identifier of the target outbox message.
    /// </summary>
    public Guid Id { get; }
}
