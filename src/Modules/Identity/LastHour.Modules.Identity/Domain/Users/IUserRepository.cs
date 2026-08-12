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

    /// <summary>
    /// Retrieves the user with the specified identifier, or <see langword="null"/> when no
    /// such user exists. Used by flows that hold a user id but not the email address (for
    /// example refresh-token validation, which re-checks the account state).
    /// </summary>
    /// <param name="id">The user identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching user, or <see langword="null"/> when not found.</returns>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the user with the specified identifier and places the aggregate in the change
    /// tracker, or <see langword="null"/> when no such user exists. Used by flows that mutate
    /// the user (authentication throttle updates, password changes) and must rely on the
    /// optimistic-concurrency token to make concurrent writes safe: the original token value is
    /// retained for the update predicate, so a lost update surfaces as a concurrency conflict.
    /// </summary>
    /// <param name="id">The user identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked user, or <see langword="null"/> when not found.</returns>
    Task<User?> GetByIdTrackedAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the tracked state of a user from the store, discarding any pending changes.
    /// After a concurrency conflict on a throttle update, this re-establishes the original
    /// values so the update can be retried against the current stored state.
    /// </summary>
    /// <param name="user">The tracked user to refresh.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReloadTrackedAsync(User user, CancellationToken cancellationToken = default);
}
