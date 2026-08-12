using LastHour.Modules.Identity.Application.RefreshTokens;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic stand-in for the real refresh-token service, which is covered by its own unit
/// tests. Application tests use this stub to exercise use-case logic only, recording every
/// generation so tests can assert what was issued and persisted.
/// </summary>
public sealed class StubRefreshTokenService : IRefreshTokenService
{
    /// <summary>
    /// Gets the number of secrets generated.
    /// </summary>
    public int GenerateCallCount { get; private set; }

    /// <inheritdoc/>
    public RefreshTokenSecret Generate()
    {
        GenerateCallCount++;
        string raw = $"stub-refresh-token-{GenerateCallCount}";
        return new RefreshTokenSecret(raw, Hash(raw));
    }

    /// <inheritdoc/>
    public string Hash(string rawToken) => "stub-hash:" + rawToken;
}
