using LastHour.BuildingBlocks.Application.Cqrs;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Authenticates an account and issues an access token and a refresh token. The use case is a
/// command because a successful login now persists a refresh-token record — the transaction
/// pipeline commits it atomically. Only the email address and the plaintext password are
/// accepted; both tokens, their claims and their lifetimes are server-owned.
/// </summary>
public sealed record LoginCommand : ICommand<LoginResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommand"/> class.
    /// </summary>
    /// <param name="email">The account email address; normalized by the domain's single rule.</param>
    /// <param name="password">The plaintext password to verify. It is never logged, returned or persisted.</param>
    public LoginCommand(string email, string password)
    {
        Email = email;
        Password = password;
    }

    /// <summary>
    /// Gets the account email address.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Gets the plaintext password to verify.
    /// </summary>
    public string Password { get; }
}
