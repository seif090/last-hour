using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the user repository, keyed by normalized email exactly
/// like the production lookup. Password-change and session-control tests drive the real
/// aggregate through this fake; the tracked reads mirror the same instance, which is all the
/// in-memory model needs.
/// </summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all stored users, for assertions.
    /// </summary>
    public IReadOnlyCollection<User> All => _users.Values;

    /// <inheritdoc/>
    public void Add(User user) => _users[user.Email.NormalizedValue] = user;

    /// <summary>
    /// Creates and stores an active user with a real hash of the given password.
    /// </summary>
    /// <param name="email">The account email address.</param>
    /// <param name="password">The password whose hash is stored.</param>
    /// <returns>The created user.</returns>
    public User AddActiveUser(string email, string password)
    {
        User user = User.Create(
            Email.From(email),
            PasswordCredential.FromHash(new StubPasswordHasher().Hash(password)));
        user.Activate();
        Add(user);
        return user;
    }

    /// <summary>
    /// Retrieves the stored user for a normalized email address.
    /// </summary>
    /// <param name="email">The address to look up.</param>
    /// <returns>The stored user.</returns>
    public User FindByEmail(string email)
        => _users[Email.From(email).NormalizedValue];

    /// <inheritdoc/>
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.GetValueOrDefault(email.NormalizedValue));

    /// <inheritdoc/>
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Values.FirstOrDefault(user => user.Id == id));

    /// <inheritdoc/>
    public Task<User?> GetByIdTrackedAsync(UserId id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task ReloadTrackedAsync(User user, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.ContainsKey(email.NormalizedValue));

    /// <inheritdoc/>
    public void Update(User user) => Add(user);

    /// <inheritdoc/>
    public void Remove(User user) => _users.Remove(user.Email.NormalizedValue);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<User> aggregateRoots)
    {
        foreach (User user in aggregateRoots)
        {
            Remove(user);
        }
    }
}
