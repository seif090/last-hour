using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic stand-in for the JWT access token generator, which is covered by the
/// infrastructure tests. Application tests use this stub to exercise use-case logic only,
/// recording which subject a token was issued for.
/// </summary>
public sealed class StubAccessTokenGenerator : IAccessTokenGenerator
{
    /// <summary>
    /// Gets the subject the most recent token was issued for.
    /// </summary>
    public Guid? LastIssuedUserId { get; private set; }

    /// <summary>
    /// Gets the number of tokens issued.
    /// </summary>
    public int GenerateCallCount { get; private set; }

    /// <inheritdoc/>
    public GeneratedAccessToken Generate(Guid userId)
    {
        LastIssuedUserId = userId;
        GenerateCallCount++;
        return new GeneratedAccessToken($"stub-token:{userId:N}", DateTimeOffset.UtcNow.AddMinutes(15));
    }
}
