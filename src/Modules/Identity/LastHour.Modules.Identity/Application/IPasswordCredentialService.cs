using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application;

/// <summary>
/// The application-level credential management capability: hashing, verification, policy
/// validation and password changes. It sits between the password hashing provider
/// (<see cref="IPasswordHasher"/>, an infrastructure concern) and future authentication flows,
/// and it is the only application surface that translates passwords into credentials. No
/// password value ever leaves these methods — hashes only.
/// </summary>
public interface IPasswordCredentialService
{
    /// <summary>
    /// Hashes a password into a <see cref="PasswordCredential"/> using the configured algorithm.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The opaque credential to persist.</returns>
    PasswordCredential HashPassword(string password);

    /// <summary>
    /// Verifies a password against a stored credential.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="credential">The stored credential to verify against.</param>
    /// <returns>The verification outcome.</returns>
    PasswordVerificationResult VerifyPassword(string password, PasswordCredential credential);

    /// <summary>
    /// Validates a prospective password against the configured <see cref="PasswordPolicy"/>.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>A successful result when the password satisfies the policy; otherwise a failed
    /// result describing the first violated rule.</returns>
    Result ValidatePassword(string? password);

    /// <summary>
    /// Changes the password of an aggregate: verifies the current password, validates the new
    /// one, hashes it and updates the credential on the aggregate. The caller owns persistence
    /// of the mutated aggregate through the unit of work, which keeps this service free of
    /// repository dependencies.
    /// </summary>
    /// <param name="user">The user whose password is changed; it is mutated in place on success.</param>
    /// <param name="currentPassword">The current password, verified before any change.</param>
    /// <param name="newPassword">The new password, validated against the policy before hashing.</param>
    /// <returns>A successful result when the password was changed; otherwise a failed result
    /// describing the first violated rule.</returns>
    Result ChangePassword(User user, string currentPassword, string newPassword);
}
