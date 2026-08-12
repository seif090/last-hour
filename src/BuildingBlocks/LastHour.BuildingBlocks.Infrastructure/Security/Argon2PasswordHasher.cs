using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;
using LastHour.BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// <see cref="IPasswordHasher"/> implemented with Argon2id (version 1.3) via the managed
/// Isopoh.Cryptography.Argon2 library. Hashes are encoded in the PHC string format
/// (<c>$argon2id$v=19$m=…,t=…,p=…$salt$hash</c>), which embeds the algorithm, parameters, salt
/// and derived key so verification is self-describing and parameters can evolve over time.
/// The provider is stateless and safe for concurrent use; it is registered as a singleton.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private readonly PasswordHashingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2PasswordHasher"/> class.
    /// </summary>
    /// <param name="options">The configured hashing parameters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public Argon2PasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc/>
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] salt = new byte[_options.SaltLength];
        RandomNumberGenerator.Fill(salt);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = _options.TimeCost,
            MemoryCost = _options.MemoryCost,
            Lanes = _options.Lanes,
            Threads = _options.Threads,
            HashLength = _options.HashLength,
            Salt = salt,
            Password = passwordBytes,
            ClearPassword = true,
        };

        return Argon2.Hash(config);
    }

    /// <inheritdoc/>
    public PasswordVerificationResult Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        bool matches;
#pragma warning disable CA1031 // Argon2.Verify throws several decoder exception types for malformed stored hashes; all must surface as a failed verification.
        try
        {
            matches = Argon2.Verify(passwordHash, password);
        }
        catch (Exception)
        {
            return PasswordVerificationResult.Failed;
        }
#pragma warning restore CA1031

        if (!matches)
        {
            return PasswordVerificationResult.Failed;
        }

        return NeedsRehash(passwordHash)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }

    /// <inheritdoc/>
    public bool NeedsRehash(string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (!TryParseParameters(
                passwordHash,
                out int version,
                out int memoryCost,
                out int timeCost,
                out int lanes))
        {
            // Unknown or unparsable formats are always candidates for an upgrade; verification
            // of such hashes fails anyway, so this path never triggers a rehash of a working
            // credential.
            return true;
        }

        return version < (int)Argon2Version.Nineteen
            || memoryCost < _options.MemoryCost
            || timeCost < _options.TimeCost
            || lanes < _options.Lanes;
    }

    private static bool TryParseParameters(
        string encoded,
        out int version,
        out int memoryCost,
        out int timeCost,
        out int lanes)
    {
        version = 0;
        memoryCost = 0;
        timeCost = 0;
        lanes = 0;

        string[] segments = encoded.Split('$');

        // $argon2id$v=19$m=…,t=…,p=…$salt$hash → exactly six segments.
        if (segments.Length < 6
            || segments[0].Length != 0
            || !string.Equals(segments[1], "argon2id", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string segment in segments)
        {
            if (segment.StartsWith("v=", StringComparison.Ordinal)
                && int.TryParse(segment.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out int parsedVersion))
            {
                version = parsedVersion;
            }
            else if (segment.Contains(','))
            {
                foreach (string pair in segment.Split(','))
                {
                    ReadOnlySpan<char> span = pair.AsSpan();
                    int separator = span.IndexOf('=');
                    if (separator <= 0)
                    {
                        return false;
                    }

                    ReadOnlySpan<char> key = span[..separator];
                    ReadOnlySpan<char> value = span.Slice(separator + 1);
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
                    {
                        return false;
                    }

                    if (key.SequenceEqual("m"))
                    {
                        memoryCost = parsed;
                    }
                    else if (key.SequenceEqual("t"))
                    {
                        timeCost = parsed;
                    }
                    else if (key.SequenceEqual("p"))
                    {
                        lanes = parsed;
                    }
                }
            }
        }

        return memoryCost > 0 && timeCost > 0 && lanes > 0;
    }
}
