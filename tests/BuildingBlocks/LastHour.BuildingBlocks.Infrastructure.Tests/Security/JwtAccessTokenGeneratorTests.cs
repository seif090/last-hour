using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Security;

public class JwtAccessTokenGeneratorTests
{
    private const string Issuer = "https://lasthour.dev";
    private const string Audience = "lasthour-api";
    private const string SigningKey = "0123456789abcdef0123456789abcdef0123456789abcdef";

    private static JwtAccessTokenGenerator CreateGenerator(TimeSpan? lifetime = null)
    {
        var options = new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            AccessTokenLifetime = lifetime ?? TimeSpan.FromMinutes(15),
        };

        return new JwtAccessTokenGenerator(Options.Create(options));
    }

    [Fact]
    public void Generate_ReturnsNonEmptyTokenWithMinimalClaims()
    {
        JwtAccessTokenGenerator generator = CreateGenerator();
        var userId = Guid.NewGuid();

        GeneratedAccessToken result = generator.Generate(userId);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Token.Should().NotContain(userId.ToString(), "the raw identifier must not leak into the token text");

        JsonWebToken token = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);
        token.Subject.Should().Be(userId.ToString("D"));
        token.Issuer.Should().Be(Issuer);
        token.Audiences.Should().Contain(Audience);
        token.Id.Should().NotBeNullOrWhiteSpace("every token carries a unique identifier");
        token.TryGetPayloadValue<string>(JwtRegisteredClaimNames.Jti, out string? jti).Should().BeTrue();
        jti.Should().Be(token.Id);
    }

    [Fact]
    public void Generate_ExpirationMatchesConfiguredLifetime()
    {
        var lifetime = TimeSpan.FromMinutes(7);
        JwtAccessTokenGenerator generator = CreateGenerator(lifetime);

        GeneratedAccessToken result = generator.Generate(Guid.NewGuid());

        JsonWebToken token = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);
        (token.ValidTo - token.ValidFrom).Should().BeCloseTo(lifetime, TimeSpan.FromSeconds(2));
        result.ExpiresAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(lifetime), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Generate_TwoTokensForSameUser_HaveDifferentIdentifiers()
    {
        JwtAccessTokenGenerator generator = CreateGenerator();
        var userId = Guid.NewGuid();

        GeneratedAccessToken first = generator.Generate(userId);
        GeneratedAccessToken second = generator.Generate(userId);

        JsonWebToken firstToken = new JsonWebTokenHandler().ReadJsonWebToken(first.Token);
        JsonWebToken secondToken = new JsonWebTokenHandler().ReadJsonWebToken(second.Token);
        firstToken.Id.Should().NotBe(secondToken.Id);
        first.Token.Should().NotBe(second.Token);
    }

    [Fact]
    public void Generate_TokenCarriesNoSensitiveClaims()
    {
        JwtAccessTokenGenerator generator = CreateGenerator();

        GeneratedAccessToken result = generator.Generate(Guid.NewGuid());

        JsonWebToken token = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);
        token.TryGetPayloadValue<string>("email", out _).Should().BeFalse("email is personally identifying data and not needed in the token");
        token.TryGetPayloadValue<string>("PasswordHash", out _).Should().BeFalse();
        token.TryGetPayloadValue<string>("SecurityStamp", out _).Should().BeFalse();
        token.TryGetPayloadValue<string>("role", out _).Should().BeFalse();
    }
}
