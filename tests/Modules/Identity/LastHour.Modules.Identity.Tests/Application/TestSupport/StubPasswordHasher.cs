using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Modules.Identity.Tests.Application.TestSupport;

/// <summary>
/// Deterministic, dependency-free stand-in for the real Argon2 provider. The provider itself is
/// covered by the infrastructure tests; application tests use this stub to exercise service
/// logic only, keeping the suite fast.
/// </summary>
public sealed class StubPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "stub:" + password;

    public PasswordVerificationResult Verify(string password, string passwordHash)
        => string.Equals(passwordHash, Hash(password), StringComparison.Ordinal)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;

    public bool NeedsRehash(string passwordHash) => false;
}
