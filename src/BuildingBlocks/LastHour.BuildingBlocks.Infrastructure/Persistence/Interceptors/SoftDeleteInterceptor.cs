using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Converts the physical deletion of <see cref="ISoftDelete"/> entities into a logical
/// deletion: the entry is left in the data store, flagged as deleted with a timestamp and
/// actor, and excluded from queries by the global query filter applied by the model.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
    /// </summary>
    /// <param name="clock">The clock used to timestamp deletions.</param>
    /// <param name="currentUser">The current user, recorded as the deletion actor.</param>
    public SoftDeleteInterceptor(IClock clock, ICurrentUser currentUser)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted || entry.Entity is not ISoftDelete softDelete)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            softDelete.IsDeleted = true;
            softDelete.DeletedAt = _clock.UtcNow;
            softDelete.DeletedBy = _currentUser.UserId;

            foreach (ReferenceEntry reference in entry.References)
            {
                if (reference.TargetEntry is not null && reference.TargetEntry.Metadata.IsOwned())
                {
                    reference.TargetEntry.State = EntityState.Unchanged;
                }
            }
        }
    }
}
