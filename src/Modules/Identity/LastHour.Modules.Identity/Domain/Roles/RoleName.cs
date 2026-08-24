using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Domain.Roles;

/// <summary>
/// The name of a <see cref="Role"/>, modeled as an immutable value object so that validation
/// and normalization live in exactly one place. The equality semantics compare the original
/// value; <see cref="NormalizedValue"/> is the single deterministic representation used for
/// uniqueness and lookups, so <c>Administrator</c> and <c>administrator</c> resolve to the
/// same role. The normalization rule mirrors the identity module's address normalization
/// (<see cref="Email.Normalize"/>): whitespace trimmed and lower-cased with the invariant
/// culture.
/// </summary>
public sealed class RoleName : ValueObject
{
    /// <summary>
    /// The maximum length of a role name.
    /// </summary>
    public const int MaxLength = 64;

    private readonly string _value;
    private readonly string _normalizedValue;

    private RoleName(string value, string normalizedValue)
    {
        _value = value;
        _normalizedValue = normalizedValue;
    }

    /// <summary>
    /// Gets the original name as supplied (whitespace-trimmed, case preserved).
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets the normalized representation: whitespace trimmed and lower-cased with the
    /// invariant culture. This value is persisted and protected by a unique index, and it is
    /// the only representation ever used in equality-sensitive queries.
    /// </summary>
    public string NormalizedValue => _normalizedValue;

    /// <summary>
    /// Creates a <see cref="RoleName"/> from a raw name, validating the domain invariant.
    /// </summary>
    /// <param name="value">The raw name to validate and normalize.</param>
    /// <returns>A new <see cref="RoleName"/>.</returns>
    /// <exception cref="InvalidRoleNameException"><paramref name="value"/> is null, empty, or
    /// longer than <see cref="MaxLength"/> characters after trimming.</exception>
    public static RoleName From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidRoleNameException("A role name is required.");
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidRoleNameException($"A role name cannot exceed {MaxLength} characters.");
        }

        return new RoleName(trimmed, Normalize(trimmed));
    }

    /// <summary>
    /// Normalizes a raw role name for comparison and storage. This is the single normalization
    /// rule for role identities: whitespace is trimmed and the name is lower-cased with the
    /// invariant culture. Callers must not re-implement normalization elsewhere.
    /// </summary>
    /// <param name="value">The name to normalize.</param>
    /// <returns>The normalized name.</returns>
    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }
}
