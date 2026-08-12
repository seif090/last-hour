using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Application;

/// <summary>
/// The default <see cref="IPasswordCredentialService"/> implementation. It applies the
/// configured <see cref="PasswordPolicy"/> and delegates hashing to the registered
/// <see cref="IPasswordHasher"/>. It deliberately does not depend on repositories, unit of
/// work or HTTP, so it is infrastructure-free and directly unit-testable.
/// </summary>
public sealed class PasswordCredentialService : IPasswordCredentialService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordCredentialService"/> class.
    /// </summary>
    /// <param name="passwordHasher">The password hashing provider.</param>
    /// <param name="policy">The configured password policy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="passwordHasher"/> or
    /// <paramref name="policy"/> is <see langword="null"/>.</exception>
    public PasswordCredentialService(IPasswordHasher passwordHasher, IOptions<PasswordPolicy> policy)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(policy);
        _passwordHasher = passwordHasher;
        _policy = policy.Value;
    }

    /// <inheritdoc/>
    public PasswordCredential HashPassword(string password)
        => PasswordCredential.FromHash(_passwordHasher.Hash(password));

    /// <inheritdoc/>
    public PasswordVerificationResult VerifyPassword(string password, PasswordCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return _passwordHasher.Verify(password, credential.Hash);
    }

    /// <inheritdoc/>
    public Result ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure(
                Error.Validation("Identity.PasswordRequired", "A password is required."));
        }

        if (password.Length < _policy.MinimumLength)
        {
            return Result.Failure(
                Error.Validation(
                    "Identity.PasswordTooShort",
                    $"The password must be at least {_policy.MinimumLength} characters long."));
        }

        if (password.Length > _policy.MaximumLength)
        {
            return Result.Failure(
                Error.Validation(
                    "Identity.PasswordTooLong",
                    $"The password cannot exceed {_policy.MaximumLength} characters."));
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public Result ChangePassword(User user, string currentPassword, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(user);

        PasswordCredential? credential = user.PasswordCredential;
        if (credential is null)
        {
            return Result.Failure(
                Error.Failure(
                    "Identity.NoPasswordCredential",
                    "The account has no password credential to change."));
        }

        if (_passwordHasher.Verify(currentPassword, credential.Hash) == PasswordVerificationResult.Failed)
        {
            return Result.Failure(
                Error.Validation("Identity.InvalidCurrentPassword", "The current password is incorrect."));
        }

        // Rejecting a change to the identical password is verified through the hasher (a
        // verification against the stored hash), never by comparing plaintext values.
        if (_passwordHasher.Verify(newPassword, credential.Hash) == PasswordVerificationResult.Success)
        {
            return Result.Failure(
                Error.Validation(
                    "Identity.PasswordUnchanged",
                    "The new password must be different from the current password."));
        }

        Result validation = ValidatePassword(newPassword);
        if (validation.IsFailure)
        {
            return validation;
        }

        user.SetPasswordCredential(PasswordCredential.FromHash(_passwordHasher.Hash(newPassword)));
        return Result.Success();
    }
}
