using System.Security.Cryptography;
using System.Text;

namespace LastHour.Modules.Identity.Application.RefreshTokens;

/// <summary>
/// The default <see cref="IRefreshTokenService"/>. Raw secrets are 256 bits of CSPRNG entropy
/// encoded URL-safe (base64url, no padding, 43 characters), and the storage hash is SHA-256
/// over the UTF-8 bytes of the raw secret (lowercase hex, 64 characters). The implementation
/// is stateless and thread-safe, so a single instance is safe for concurrent use.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenByteLength = 32;

    /// <inheritdoc/>
    public RefreshTokenSecret Generate()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        string rawToken = Base64UrlEncode(bytes);
        return new RefreshTokenSecret(rawToken, Hash(rawToken));
    }

    /// <inheritdoc/>
    public string Hash(string rawToken)
    {
        ArgumentNullException.ThrowIfNull(rawToken);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
