using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// The persistence contract of the <see cref="User"/> aggregate. It extends the shared write
/// repository with the identity-specific lookups that future authentication flows need.
/// Implementations must resolve by the normalized email value only, so lookups never depend on
/// the case or whitespace of the caller's input.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Retrieves the user with the specified email address, or <see langword="null"/> when no
    /// such user exists.
    /// </summary>
    /// <param name="email">The email address to look up; only its normalized value is used.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching user, or <see langword="null"/> when not found.</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a user with the specified email address already exists.
    /// </summary>
    /// <param name="email">The email address to check; only its normalized value is used.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when a user with the normalized address exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
}
