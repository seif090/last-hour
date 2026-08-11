using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// Represents the password credential of a <see cref="User"/> as the opaque result of a
/// password-hashing scheme. The aggregate only ever holds the final hash representation —
/// plaintext passwords, raw secrets and confirmation values never enter the domain. Hashing
/// itself is not implemented in this sprint; a future authentication sprint will produce the
/// hash (for example via Argon2 or bcrypt) and construct this value with
/// <see cref="FromHash"/>.
/// </summary>
public sealed class PasswordCredential : ValueObject
{
    /// <summary>
    /// The maximum length of a stored hash. Modern password hashes (Argon2id, bcrypt, PBKDF2)
    /// serialize to well under this bound; the headroom avoids artificial constraints on the
    /// hashing configuration chosen later.
    /// </summary>
    public const int MaxHashLength = 512;

    private readonly string _hash;

    private PasswordCredential(string hash)
    {
        _hash = hash;
    }

    /// <summary>
    /// Gets the password hash. This value is persisted and must never be exposed through API
    /// responses or logs; the mapping keeps it in the database only.
    /// </summary>
    public string Hash => _hash;

    /// <summary>
    /// Creates a credential from an already-computed hash.
    /// </summary>
    /// <param name="hash">The hash produced by the future hashing implementation.</param>
    /// <returns>A new <see cref="PasswordCredential"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="hash"/> is null, empty or exceeds
    /// <see cref="MaxHashLength"/> characters.</exception>
    public static PasswordCredential FromHash(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("A password hash is required.", nameof(hash));
        }

        if (hash.Length > MaxHashLength)
        {
            throw new ArgumentException(
                $"A password hash cannot exceed {MaxHashLength} characters.",
                nameof(hash));
        }

        return new PasswordCredential(hash);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _hash;
    }
}
