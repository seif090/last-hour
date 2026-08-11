using System.Net.Mail;
using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// The email address of a <see cref="User"/>, modeled as an immutable value object so that
/// validation and normalization live in exactly one place. The equality semantics compare the
/// original (trimmed) address; <see cref="NormalizedValue"/> is the single deterministic
/// representation used for uniqueness and lookups, so <c>User@Example.com</c> and
/// <c>user@example.com</c> resolve to the same account.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>
    /// The maximum length of an email address as specified by RFC 5321.
    /// </summary>
    public const int MaxLength = 254;

    private readonly string _value;
    private readonly string _normalizedValue;

    private Email(string value, string normalizedValue)
    {
        _value = value;
        _normalizedValue = normalizedValue;
    }

    /// <summary>
    /// Gets the original address as supplied (whitespace-trimmed, case preserved).
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Gets the normalized representation: whitespace trimmed and lower-cased with the
    /// invariant culture. This value is persisted and protected by a unique index, and it is
    /// the only representation ever used in equality-sensitive queries.
    /// </summary>
    public string NormalizedValue => _normalizedValue;

    /// <summary>
    /// Creates an <see cref="Email"/> from a raw address, validating the domain invariant.
    /// </summary>
    /// <param name="value">The raw address to validate and normalize.</param>
    /// <returns>A new <see cref="Email"/>.</returns>
    /// <exception cref="InvalidEmailException"><paramref name="value"/> is null, empty, longer
    /// than <see cref="MaxLength"/> characters after trimming, or not a valid address.</exception>
    public static Email From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidEmailException("An email address is required.");
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidEmailException($"An email address cannot exceed {MaxLength} characters.");
        }

        if (!IsValidFormat(trimmed))
        {
            throw new InvalidEmailException("The value is not a valid email address.");
        }

        return new Email(trimmed, Normalize(trimmed));
    }

    /// <summary>
    /// Normalizes a raw address for comparison and storage. This is the single normalization
    /// rule for the identity module: whitespace is trimmed and the address is lower-cased with
    /// the invariant culture. Callers must not re-implement normalization elsewhere.
    /// </summary>
    /// <param name="value">The address to normalize.</param>
    /// <returns>The normalized address.</returns>
    public static string Normalize(string value) => value.Trim().ToLowerInvariant();

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    private static bool IsValidFormat(string value)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
