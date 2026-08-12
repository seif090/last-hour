using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Identity.Domain.Users;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Registers a new user account. Only client-controlled values are accepted: the email address
/// and the plaintext password (which never leaves the use case as anything but a hash).
/// Everything else — the identifier, account status, audit stamps and concurrency token — is
/// owned by the domain and persistence layers and cannot be supplied through this command.
/// </summary>
public sealed record RegisterUserCommand : ICommand<UserId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserCommand"/> class.
    /// </summary>
    /// <param name="email">The account email address; normalized by the domain's single rule.</param>
    /// <param name="password">The plaintext password, validated against the policy and hashed before persistence.</param>
    public RegisterUserCommand(string email, string password)
    {
        Email = email;
        Password = password;
    }

    /// <summary>
    /// Gets the account email address.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Gets the plaintext password.
    /// </summary>
    public string Password { get; }
}
