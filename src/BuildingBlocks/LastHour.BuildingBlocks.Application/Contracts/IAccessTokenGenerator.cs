namespace LastHour.BuildingBlocks.Application.Contracts;

/// <summary>
/// Issues short-lived access tokens for an authenticated subject. This is the contract consumed
/// by authentication use cases; the concrete provider (JWT) is an infrastructure concern. The
/// generated token carries only the minimal identity claims the consumer designed — never
/// passwords, hashes, security state or personal data — and callers must never log or persist
/// the returned token.
/// </summary>
public interface IAccessTokenGenerator
{
    /// <summary>
    /// Generates a new access token for the given subject.
    /// </summary>
    /// <param name="userId">The stable identifier of the authenticated subject.</param>
    /// <returns>The generated token and its expiration, which the caller maps onto the public response.</returns>
    GeneratedAccessToken Generate(Guid userId);
}
