namespace LastHour.Api.Tests.Support;

/// <summary>
/// The JWT values the API test hosts are configured with. The signing key is a test-only
/// value never used outside the test suite; the issuer and audience mirror the production
/// defaults declared in appsettings.json so tests exercise the real configuration path.
/// </summary>
public static class TestJwt
{
    /// <summary>
    /// Gets the test-only signing key the test host validates tokens with.
    /// </summary>
    public const string SigningKey = "test-only-signing-key-0123456789abcdef0123456789abcdef";

    /// <summary>
    /// Gets the issuer the test host issues and validates tokens for.
    /// </summary>
    public const string Issuer = "https://lasthour.dev";

    /// <summary>
    /// Gets the audience the test host issues and validates tokens for.
    /// </summary>
    public const string Audience = "lasthour-api";
}
