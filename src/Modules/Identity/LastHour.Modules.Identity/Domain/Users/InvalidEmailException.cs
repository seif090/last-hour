using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// Thrown when an email address violates the domain email invariant: it must be a
/// syntactically valid address of at most <see cref="Email.MaxLength"/> characters after
/// trimming. This is a domain invariant violation, distinct from request-level validation
/// failures, which the application layer reports through <c>Result</c> errors.
/// </summary>
public sealed class InvalidEmailException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEmailException"/> class.
    /// </summary>
    /// <param name="message">The reason the email address is invalid.</param>
    public InvalidEmailException(string message)
        : base(message)
    {
    }
}
