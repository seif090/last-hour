using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Coordinates user registration: email normalization and format validation (the domain's
/// single rule), password policy validation, password hashing, the duplicate-email
/// pre-check, user creation through the aggregate and registration for persistence. The
/// atomic commit happens in the transaction pipeline behavior; the database unique index is
/// the correctness backstop when two registrations race, and the pipeline translates that
/// violation into a conflict result. No password or hash value is ever logged.
/// </summary>
public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, UserId>
{
    private static readonly Action<ILogger, Guid, string, Exception?> RegistrationAccepted =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(1001, nameof(RegisterUserCommandHandler)),
            "Registration accepted for user {UserId} with status {Status}.");

    private static readonly Action<ILogger, Exception?> DuplicateRegistrationRejected =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1002, nameof(RegisterUserCommandHandler)),
            "Registration rejected because the normalized email address is already registered.");

    private readonly IUserRepository _users;
    private readonly IPasswordCredentialService _credentials;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used for the duplicate pre-check and persistence.</param>
    /// <param name="credentials">The credential service that validates and hashes passwords.</param>
    /// <param name="logger">The logger used to record registration events without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RegisterUserCommandHandler(
        IUserRepository users,
        IPasswordCredentialService credentials,
        ILogger<RegisterUserCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(logger);
        _users = users;
        _credentials = credentials;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<UserId>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(command.Email);
        }
        catch (InvalidEmailException exception)
        {
            return Result<UserId>.Failure(
                Error.Validation("InvalidEmail", exception.Message));
        }

        Result passwordValidation = _credentials.ValidatePassword(command.Password);
        if (passwordValidation.IsFailure)
        {
            return Result<UserId>.Failure(passwordValidation.FirstError!);
        }

        if (await _users.ExistsByEmailAsync(email, cancellationToken).ConfigureAwait(false))
        {
            DuplicateRegistrationRejected(_logger, null);
            return Result<UserId>.Failure(Error.Conflict(
                "Identity.EmailAlreadyRegistered",
                "An account with this email address already exists."));
        }

        User user = User.Create(email, _credentials.HashPassword(command.Password));
        _users.Add(user);

        RegistrationAccepted(_logger, user.Id.Value, user.Status.ToString(), null);
        return Result<UserId>.Success(user.Id);
    }
}
