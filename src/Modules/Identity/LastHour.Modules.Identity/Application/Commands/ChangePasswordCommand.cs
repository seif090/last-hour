using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Changes the authenticated user's password: the current password is verified, the new password
/// is validated against the policy and hashed, and every outstanding session is revoked so the
/// new credential takes effect everywhere at once. The user identifier is injected by the API
/// layer from the validated access-token claims — never accepted from the request body. Both
/// passwords are plaintext inputs that are verified and hashed in place; neither is ever logged,
/// returned or persisted.
/// </summary>
public sealed record ChangePasswordCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordCommand"/> class.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier, resolved from the access-token claims.</param>
    /// <param name="currentPassword">The current password, verified before any change.</param>
    /// <param name="newPassword">The new password, validated against the policy before hashing.</param>
    public ChangePasswordCommand(Guid userId, string currentPassword, string newPassword)
    {
        UserId = userId;
        CurrentPassword = currentPassword;
        NewPassword = newPassword;
    }

    /// <summary>
    /// Gets the authenticated user's identifier.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the current password to verify.
    /// </summary>
    public string CurrentPassword { get; }

    /// <summary>
    /// Gets the new password to validate and store.
    /// </summary>
    public string NewPassword { get; }
}
