using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Events;

/// <summary>
/// Domain event raised when a user's password credential has been successfully changed.
/// </summary>
public sealed class UserPasswordChangedDomainEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPasswordChangedDomainEvent"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose password was changed.</param>
    public UserPasswordChangedDomainEvent(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// Gets the unique identifier of the user whose password was changed.
    /// </summary>
    public Guid UserId { get; }
}
