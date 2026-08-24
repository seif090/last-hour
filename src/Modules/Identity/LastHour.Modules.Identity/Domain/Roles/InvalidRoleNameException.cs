using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// Thrown when a role name violates the domain role-name invariant: it must be non-empty and
/// at most <see cref="RoleName.MaxLength"/> characters after trimming. This is a domain
/// invariant violation, distinct from request-level validation failures, which the application
/// layer reports through <c>Result</c> errors.
/// </summary>
public sealed class InvalidRoleNameException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRoleNameException"/> class.
    /// </summary>
    /// <param name="message">The reason the role name is invalid.</param>
    public InvalidRoleNameException(string message)
        : base(message)
    {
    }
}
