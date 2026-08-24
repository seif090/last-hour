using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Events;

/// <summary>
/// Domain event raised when a user account transitions into a temporary locked-out state
/// after reaching the maximum consecutive failed authentication attempts.
/// </summary>
public sealed class UserLockedOutDomainEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserLockedOutDomainEvent"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who was locked out.</param>
    /// <param name="lockoutUntilUtc">The timestamp until which the account remains locked, in UTC.</param>
    public UserLockedOutDomainEvent(
        Guid userId,
        DateTime lockoutUntilUtc)
    {
        UserId = userId;
        LockoutUntilUtc = lockoutUntilUtc;
    }

    /// <summary>
    /// Gets the unique identifier of the user who was locked out.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the timestamp until which the account remains locked, in UTC.
    /// </summary>
    public DateTime LockoutUntilUtc { get; }
}
