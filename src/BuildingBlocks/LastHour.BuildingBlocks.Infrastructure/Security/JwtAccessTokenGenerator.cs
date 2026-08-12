using System.Text;
using LastHour.BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LastHour.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Issues short-lived JWT access tokens signed with the configured symmetric key (HS256). The
/// token carries exactly three claims by design — <c>sub</c> (the stable user identifier),
/// <c>jti</c> (a unique token identifier for future revocation and audit correlation) and the
/// standard <c>iat</c>/<c>exp</c> timestamps — plus the configured issuer and audience. No
/// email, password material, security state or personal data is ever embedded in the token.
/// The handler is stateless, so a single instance is safe for concurrent use.
/// </summary>
public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly JsonWebTokenHandler _handler = new JsonWebTokenHandler();

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAccessTokenGenerator"/> class.
    /// </summary>
    /// <param name="options">The JWT configuration, validated at startup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public JwtAccessTokenGenerator(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc/>
    public GeneratedAccessToken Generate(Guid userId)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime expiresAt = utcNow.Add(_options.AccessTokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = utcNow,
            NotBefore = utcNow,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString("D"),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [JwtRegisteredClaimNames.Iat] = new DateTimeOffset(utcNow, TimeSpan.Zero).ToUnixTimeSeconds(),
            },
        };

        string token = _handler.CreateToken(descriptor);
        return new GeneratedAccessToken(token, new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }
}
