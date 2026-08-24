using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Commands;

/// <summary>
/// Administrative command that resets an abandoned outbox message so that it is re-queued
/// for processing immediately.
/// </summary>
public sealed record RequeueOutboxMessageCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueOutboxMessageCommand"/> class.
    /// </summary>
    /// <param name="outboxMessageId">The unique identifier of the outbox message to re-queue.</param>
    public RequeueOutboxMessageCommand(Guid outboxMessageId)
    {
        OutboxMessageId = outboxMessageId;
    }

    /// <summary>
    /// Gets the unique identifier of the outbox message to re-queue.
    /// </summary>
    public Guid OutboxMessageId { get; }
}
