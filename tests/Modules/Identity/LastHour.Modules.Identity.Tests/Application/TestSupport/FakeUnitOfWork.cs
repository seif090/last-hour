using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// In-memory unit of work for command tests: transaction methods are no-ops, and
/// <see cref="SaveChangesAsync"/> records every invocation so handlers that commit
/// explicitly (the refresh reuse path) can be asserted.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Gets the number of times <see cref="SaveChangesAsync"/> was called.
    /// </summary>
    public int SaveChangesCount { get; private set; }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.FromResult(0);
    }

    /// <inheritdoc/>
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
