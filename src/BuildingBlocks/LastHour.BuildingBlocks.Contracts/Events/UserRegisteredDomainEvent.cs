using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Events;

/// <summary>
/// Domain event raised when a new user account is successfully registered on the platform.
/// </summary>
public sealed class UserRegisteredDomainEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRegisteredDomainEvent"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the registered user.</param>
    /// <param name="email">The registered user email address.</param>
    public UserRegisteredDomainEvent(
        Guid userId,
        string email)
    {
        UserId = userId;
        Email = email;
    }

    /// <summary>
    /// Gets the unique identifier of the registered user.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the registered user email address.
    /// </summary>
    public string Email { get; }
}
