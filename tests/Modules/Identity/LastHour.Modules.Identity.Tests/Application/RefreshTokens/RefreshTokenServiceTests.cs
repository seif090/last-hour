using FluentAssertions;
using LastHour.Modules.Identity.Application.RefreshTokens;

namespace LastHour.Modules.Identity.Tests.Application.RefreshTokens;

public class RefreshTokenServiceTests
{
    private readonly RefreshTokenService _service = new RefreshTokenService();

    [Fact]
    public void Generate_ReturnsUrlSafeTokenOfExpectedEntropy()
    {
        RefreshTokenSecret secret = _service.Generate();

        secret.RawToken.Should().NotBeNullOrWhiteSpace();
        secret.RawToken.Length.Should().Be(43, "32 bytes of entropy encoded as unpadded base64url");
        secret.RawToken.Should().MatchRegex("^[A-Za-z0-9_-]+$", "the token is URL-safe");
        secret.RawToken.Should().NotContain("=");
    }

    [Fact]
    public void Generate_ProducesUniqueTokens()
    {
        var secrets = Enumerable.Range(0, 100)
            .Select(_ => _service.Generate().RawToken)
            .ToList();

        secrets.Distinct().Should().HaveCount(100, "a security credential must never repeat");
    }

    [Fact]
    public void Generate_RawTokenIsNeverRecoverableFromHash()
    {
        RefreshTokenSecret secret = _service.Generate();

        secret.TokenHash.Should().NotBe(secret.RawToken);
        secret.TokenHash.Should().NotContain(secret.RawToken);
    }

    [Fact]
    public void Hash_IsDeterministicAndDigestSized()
    {
        string first = _service.Hash("the-same-raw-token");
        string second = _service.Hash("the-same-raw-token");

        first.Should().Be(second);
        first.Length.Should().Be(64, "SHA-256 lowercase hex");
        first.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Hash_DifferentTokens_ProduceDifferentHashes()
    {
        _service.Hash("token-one").Should().NotBe(_service.Hash("token-two"));
    }
}
